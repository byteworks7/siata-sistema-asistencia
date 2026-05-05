/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts,scss}"],
  theme: {
    extend: {
      colors: {
        'talma-azul':  '#003087',
        'talma-verde': '#6aaa00',
        'talma-verde-btn': '#16a34a',
      }
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
  ],
}