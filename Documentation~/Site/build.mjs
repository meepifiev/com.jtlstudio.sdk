import { mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, extname, join } from "node:path";
import { fileURLToPath } from "node:url";

const siteFolder = dirname(fileURLToPath(import.meta.url));
const packageRoot = join(siteFolder, "..", "..");
const version = JSON.parse(readFileSync(join(packageRoot, "package.json"), "utf8")).version;
const repository = "https://github.com/meepifiev/com.jtlstudio.sdk";
const siteUrl = "https://meepifiev.github.io/com.jtlstudio.sdk/";
const mimeTypes = { ".png": "image/png", ".jpg": "image/jpeg", ".svg": "image/svg+xml" };
const referenceTypes = ["AdResult", "PurchaseResult", "DataState", "Capability", "DeviceType", "ModuleState", "PlatformId", "Language"];
const referenceStructs = ["LeaderboardEntry", "LeaderboardPage", "ProductPrice"];

function escapeHtml(text) {
  return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

function unescapeHtml(text) {
  return text.replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, "\"").replace(/&nbsp;/g, " ").replace(/&amp;/g, "&");
}

function example(name) {
  return escapeHtml(readFileSync(join(packageRoot, "Samples~", "Examples", name + ".cs"), "utf8").trimEnd());
}

function image(path) {
  return "data:" + mimeTypes[extname(path)] + ";base64," + readFileSync(join(packageRoot, path)).toString("base64");
}

function inline(html) {
  const text = html
    .replace(/<code>([\s\S]*?)<\/code>/g, (match, code) => "`" + code + "`")
    .replace(/<b>([\s\S]*?)<\/b>/g, (match, bold) => "**" + bold + "**")
    .replace(/<br\s*\/?>/g, ", ")
    .replace(/<[^>]+>/g, "");
  return unescapeHtml(text).replace(/\s+/g, " ").trim();
}

function table(html) {
  const rows = [...html.matchAll(/<tr>([\s\S]*?)<\/tr>/g)].map((row) => [...row[1].matchAll(/<t[hd][^>]*>([\s\S]*?)<\/t[hd]>/g)].map((cell) => inline(cell[1]).replace(/\|/g, "\\|")));
  const lines = rows.map((cells) => "| " + cells.join(" | ") + " |");
  lines.splice(1, 0, "|" + rows[0].map(() => " --- ").join("|") + "|");
  return lines.join("\n");
}

function sectionMarkdown(sectionHtml) {
  const blocks = [];
  const pattern = /<(h2|h3|h4|p|ul|ol|pre|table)(\s[^>]*)?>([\s\S]*?)<\/\1>|<div class="note[^"]*">[\s\S]*?<span>([\s\S]*?)<\/span><\/div>/g;

  for (const match of sectionHtml.matchAll(pattern)) {
    const [, tag, , body, note] = match;

    if (note !== undefined) {
      blocks.push("> " + inline(note));
    } else if (tag === "h2") {
      blocks.push("## " + inline(body));
    } else if (tag === "h3") {
      blocks.push("### " + inline(body));
    } else if (tag === "h4") {
      blocks.push("#### " + inline(body));
    } else if (tag === "p" && body.includes("<button") === false) {
      blocks.push(inline(body));
    } else if (tag === "ul") {
      blocks.push([...body.matchAll(/<li>([\s\S]*?)<\/li>/g)].map((item) => "- " + inline(item[1])).join("\n"));
    } else if (tag === "ol") {
      blocks.push([...body.matchAll(/<li>([\s\S]*?)<\/li>/g)].map((item, index) => (index + 1) + ". " + inline(item[1])).join("\n"));
    } else if (tag === "pre") {
      const language = (body.match(/class="language-(\w+)"/) || [])[1] || "";
      const code = unescapeHtml(body.replace(/<[^>]+>/g, ""));
      blocks.push("```" + (language === "plaintext" ? "" : language) + "\n" + code + "\n```");
    } else if (tag === "table") {
      blocks.push(table(body));
    }
  }

  return blocks.join("\n\n");
}

function sourceFiles(folder) {
  return readdirSync(folder, { withFileTypes: true }).flatMap((entry) => {
    const path = join(folder, entry.name);
    return entry.isDirectory() ? sourceFiles(path) : entry.name.endsWith(".cs") ? [path] : [];
  });
}

function declaration(source, kind, name) {
  const start = source.search(new RegExp("public (?:readonly )?" + kind + " " + name + "\\b"));

  if (start < 0) {
    return null;
  }

  const lines = source.slice(start).split("\n");
  const end = lines.findIndex((line) => line === "    }");
  return lines.slice(0, end + 1).map((line) => line.replace(/^    /, "")).join("\n");
}

function apiReference() {
  const files = sourceFiles(join(packageRoot, "Runtime"));
  const sources = files.map((path) => readFileSync(path, "utf8"));
  const parts = [];
  const facade = readFileSync(join(packageRoot, "Runtime", "Facade", "JTLSDK.cs"), "utf8");
  const facadeMembers = facade.split("\n").filter((line) => /^\s+public (static|const)/.test(line) && line.includes(" class ") === false).map((line) => "    " + line.trim().replace(/\s*=>.*$/, ";").replace(/\)\s*$/, ");"));
  parts.push("```csharp\npublic static class JTLSDK\n{\n" + facadeMembers.join("\n") + "\n}\n```");

  const interfaces = [];
  const moduleSources = sourceFiles(join(packageRoot, "Runtime", "Modules")).map((path) => readFileSync(path, "utf8"));

  for (const source of moduleSources) {
    for (const match of source.matchAll(/public interface (I\w+)/g)) {
      interfaces.push({ name: match[1], text: declaration(source, "interface", match[1]) });
    }
  }

  interfaces.sort((left, right) => left.name.localeCompare(right.name));
  parts.push(...interfaces.map((entry) => "```csharp\n" + entry.text + "\n```"));

  for (const name of referenceTypes) {
    const source = sources.find((text) => new RegExp("public enum " + name + "\\b").test(text));

    if (source) {
      parts.push("```csharp\n" + declaration(source, "enum", name) + "\n```");
    }
  }

  for (const name of referenceStructs) {
    const source = sources.find((text) => new RegExp("public readonly struct " + name + "\\b").test(text));

    if (source) {
      const members = source.split("\n").filter((line) => /^\s+public [\w<>?,\s]+ \w+ \{ get; \}/.test(line)).map((line) => "    " + line.trim());
      parts.push("```csharp\npublic readonly struct " + name + "\n{\n" + members.join("\n") + "\n}\n```");
    }
  }

  return parts.join("\n\n");
}

function llms(html) {
  const main = html.slice(html.indexOf("<main"), html.indexOf("</main>"));
  const sections = [...main.matchAll(/<section id="([\w-]+)"[^>]*>([\s\S]*?)<\/section>/g)].filter((section) => section[1] !== "ai");
  const guide = readFileSync(join(packageRoot, "Documentation~", "Guide.en.md"), "utf8").replace(/^# .*\n/, "").trim();
  const rules = readFileSync(join(siteFolder, "llms-rules.md"), "utf8").replace(/\{\{VERSION\}\}/g, version).trim();

  return [
    "# JTL SDK Documentation",
    "",
    "Canonical install URL: " + repository + ".git#v" + version,
    "Repository: " + repository,
    "Documentation: " + siteUrl,
    "Version: " + version,
    "Updated: " + new Date().toISOString().slice(0, 10),
    "",
    rules,
    "",
    "## Russian",
    "",
    sections.map((section) => sectionMarkdown(section[2])).join("\n\n"),
    "",
    "## English",
    "",
    guide,
    "",
    "## API reference",
    "",
    "Public API of JTL SDK " + version + ", taken from the package source.",
    "",
    apiReference(),
    ""
  ].join("\n").replace(/\n{3,}/g, "\n\n");
}

let html = readFileSync(join(siteFolder, "template.html"), "utf8");
html = html.replace(/\{\{EXAMPLE (\w+)\}\}/g, (match, name) => example(name));
html = html.replace(/\{\{IMAGE ([^}]+)\}\}/g, (match, path) => image(path));
html = html.replace(/\{\{VERSION\}\}/g, version);

const leftover = html.match(/\{\{[^}]+\}\}/);

if (leftover) {
  throw new Error("Unresolved placeholder " + leftover[0]);
}

const llmsText = llms(html);
const outIndex = process.argv.indexOf("--out");

if (outIndex >= 0) {
  const outFolder = process.argv[outIndex + 1];
  const document = "<!doctype html>\n<html lang=\"ru\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width,initial-scale=1,viewport-fit=cover\">\n</head>\n<body>\n" + html + "\n</body>\n</html>\n";
  mkdirSync(outFolder, { recursive: true });
  writeFileSync(join(outFolder, "index.html"), document);
  writeFileSync(join(outFolder, "llms.txt"), llmsText);
  console.log("Built " + outFolder + " for " + version);
} else {
  writeFileSync(join(siteFolder, "index.html"), html);
  writeFileSync(join(siteFolder, "llms.txt"), llmsText);
  console.log("Built index.html and llms.txt for " + version);
}
