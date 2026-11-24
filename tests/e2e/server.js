// Mimics GitHub Pages: static files, and 404.html (status 404) for unknown paths.
const http = require('http'), fs = require('fs'), path = require('path');
const root = process.argv[2];
const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.wasm': 'application/wasm', '.png': 'image/png', '.webmanifest': 'application/manifest+json', '.dat': 'application/octet-stream', '.dll': 'application/octet-stream', '.pdb': 'application/octet-stream' };
http.createServer((req, res) => {
  let p = path.join(root, decodeURIComponent(req.url.split('?')[0]));
  if (fs.existsSync(p) && fs.statSync(p).isDirectory()) p = path.join(p, 'index.html');
  let status = 200;
  if (!fs.existsSync(p)) { p = path.join(root, 'Workoutapp/404.html'); status = 404; }
  res.writeHead(status, { 'Content-Type': types[path.extname(p)] || 'application/octet-stream' });
  fs.createReadStream(p).pipe(res);
}).listen(8080);
