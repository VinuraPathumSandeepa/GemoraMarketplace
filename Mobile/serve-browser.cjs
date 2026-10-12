const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, 'build/web');
const types = { '.html': 'text/html', '.js': 'text/javascript', '.json': 'application/json', '.css': 'text/css', '.wasm': 'application/wasm', '.png': 'image/png', '.svg': 'image/svg+xml', '.ttf': 'font/ttf', '.woff2': 'font/woff2' };
http.createServer((req, res) => {
  if (/^\/(api|uploads)(\/|\?|$)/i.test(req.url)) {
    const upstream = http.request({ hostname: '127.0.0.1', port: 5198, path: req.url, method: req.method, headers: { ...req.headers, host: 'localhost:5198' } }, response => {
      res.writeHead(response.statusCode, response.headers);
      response.pipe(res);
    });
    upstream.on('error', () => { res.writeHead(502); res.end('The Gemora backend is unavailable on port 5198.'); });
    req.pipe(upstream);
    return;
  }
  let relative;
  try { relative = decodeURIComponent(new URL(req.url, 'http://localhost').pathname); }
  catch { res.writeHead(400); res.end(); return; }
  let file = path.resolve(root, '.' + relative);
  if (file !== root && !file.startsWith(root + path.sep)) { res.writeHead(403); res.end(); return; }
  if (file === root) file = path.join(root, 'index.html');
  fs.stat(file, (error, info) => {
    if (error || !info.isFile()) { res.writeHead(404); res.end(); return; }
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
    fs.createReadStream(file).pipe(res);
  });
}).listen(5174, '127.0.0.1', () => console.log('Gemora Mobile: http://localhost:5174'));
