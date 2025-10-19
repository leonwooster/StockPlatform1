/** @type {import('tailwindcss').Config} */
export default {
  content: [
    './index.html',
    './src/**/*.{vue,js,ts,jsx,tsx}'
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eef5ff',
          100: '#d9e8ff',
          200: '#b5d0ff',
          300: '#8fb7ff',
          400: '#6a9eff',
          500: '#487fff',
          600: '#2d63e6',
          700: '#1f4ac0',
          800: '#163698',
          900: '#10256f'
        },
        accent: '#ffb86b'
      },
      boxShadow: {
        soft: '0 10px 30px -10px rgba(23, 43, 77, 0.25)'
      }
    }
  },
  plugins: []
}
