const [major, minor] = process.versions.node.split('.').map(Number);
const supported = (major === 20 && minor >= 19)
  || (major === 22 && minor >= 12)
  || major === 24;
console.log(`Node.js: ${process.version}`);
if (!supported) {
  console.error('This Angular 20 project requires Node 20.19+, 22.12+, or 24.x.');
  process.exitCode = 1;
} else {
  console.log('Node version is compatible.');
}
