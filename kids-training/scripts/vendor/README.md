# Vendored browser runtimes

These browser-ready production files are kept local so the learning page can
start without an internet connection:

- React 18.3.1 (`react-18.3.1.production.min.js`)
- ReactDOM 18.3.1 (`react-dom-18.3.1.production.min.js`)
- Babel Standalone 7.26.4 (`babel-standalone-7.26.4.min.js`), loaded only when
  a non-static Design Component imports JSX or TypeScript

The files are copied unchanged from their corresponding npm packages. Package
integrity was checked against the `dist.integrity` value published by npm.
The applicable MIT license texts are included alongside the files.
