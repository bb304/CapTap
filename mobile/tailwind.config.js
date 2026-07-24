/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{js,jsx,ts,tsx}"],
  presets: [require("nativewind/preset")],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: "#3B82A0",
          soft: "#E8F2FA",
          dark: "#2A5F78",
        },
        success: "#2F9E6E",
        warning: "#D97706",
        danger: "#C44747",
        surface: "#FFFFFF",
        muted: "#F3F5F7",
        ink: "#1C2430",
        "ink-muted": "#5B6573",
      },
      fontSize: {
        body: ["18px", { lineHeight: "26px" }],
        title: ["28px", { lineHeight: "34px" }],
      },
      borderRadius: {
        card: "16px",
        button: "14px",
      },
      minHeight: {
        touch: "44px",
      },
      minWidth: {
        touch: "44px",
      },
    },
  },
  plugins: [],
};
