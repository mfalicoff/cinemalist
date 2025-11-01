/** @type {import('tailwindcss').Config} */
export default {
  content: ["./src/**/*.{html,js,svelte,ts}"],
  safelist: [
    "bg-primary-500",
    "bg-primary-600",
    "bg-primary-700",
    "text-primary-500",
    "text-primary-600",
    "text-primary-700",
    "from-primary-500",
    "to-primary-700",
    "border-primary-500",
    "hover:bg-primary-600",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          500: "#667eea",
          600: "#5568d3",
          700: "#764ba2",
        },
      },
    },
  },
  plugins: [],
};
