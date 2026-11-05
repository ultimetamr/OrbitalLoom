// Offline atlas generation from the pinned Lucide source SVGs. No player dependency.
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('C:/Users/Administrator/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const names = ['undo-2', 'rotate-ccw', 'lightbulb', 'orbit', 'book-open', 'focus', 'settings-2', 'trash-2', 'arrow-left', 'play', 'chevron-right', 'check'];
async function main() {
  const layers = [];
  for (let i = 0; i < names.length; i++) {
    const svg = fs.readFileSync(path.join(__dirname, 'UIIcons', names[i] + '.svg'), 'utf8').replaceAll('currentColor', '#ffffff');
    const input = await sharp(Buffer.from(svg)).resize(104, 104).png().toBuffer();
    layers.push({ input, left: (i % 4) * 128 + 12, top: Math.floor(i / 4) * 128 + 12 });
  }
  await sharp({ create: { width: 512, height: 384, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
    .composite(layers).png().toFile(path.join(__dirname, '../Assets/OrbitalLoomGenerated/UI/LucideAtlas.png'));
  console.log('Generated 12 Lucide icons: 512x384 RGBA atlas.');
}
main().catch(error => { console.error(error); process.exitCode = 1; });
