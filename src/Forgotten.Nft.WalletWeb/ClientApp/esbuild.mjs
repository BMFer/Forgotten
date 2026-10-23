import { build } from 'esbuild';

// Bundles the verification page logic (injected + TipLink wallets) into a single
// browser script served from wwwroot. Solana/TipLink libs assume Node globals, so
// we polyfill Buffer and map `global` to the browser global object.
await build({
  entryPoints: ['src/verify.js'],
  bundle: true,
  format: 'iife',
  target: ['es2020'],
  platform: 'browser',
  minify: true,
  sourcemap: false,
  outfile: '../wwwroot/js/verify.js',
  inject: ['./buffer-shim.js'],
  define: {
    'process.env.NODE_ENV': '"production"',
    global: 'globalThis',
  },
  logLevel: 'info',
});

console.log('Built wwwroot/js/verify.js');
