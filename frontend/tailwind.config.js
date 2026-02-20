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
      fontFamily: {
        outfit: ['"Outfit"', 'sans-serif'],
      },
      colors: {
        primary: {
          400: "#8b5cf6",
          500: "#7c3aed",
          600: "#6d28d9",
          700: "#5b21b6",
        },
        dark: {
          800: "#13131a",
          900: "#0a0a0f",
          950: "#050508",
        },
        glass: {
          light: "rgba(255, 255, 255, 0.05)",
          base: "rgba(255, 255, 255, 0.08)",
          hover: "rgba(255, 255, 255, 0.12)",
        }
      },
      animation: {
        'fade-in': 'fade-in 0.4s ease-out forwards',
        'slide-up': 'slide-up 0.5s cubic-bezier(0.16, 1, 0.3, 1) forwards',
        'glow': 'glow 2s ease-in-out infinite alternate',
      },
      keyframes: {
        'fade-in': {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        'slide-up': {
          '0%': { opacity: '0', transform: 'translateY(20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        'glow': {
          '0%': { boxShadow: '0 0 10px rgba(124, 58, 237, 0.5)' },
          '100%': { boxShadow: '0 0 20px rgba(124, 58, 237, 0.8), 0 0 30px rgba(139, 92, 246, 0.6)' },
        }
      },
    },
  },
  plugins: [],
};
