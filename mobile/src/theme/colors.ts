export const colors = {
  primary: "#3B82A0",
  primarySoft: "#E8F2FA",
  primaryDark: "#2A5F78",
  success: "#2F9E6E",
  warning: "#D97706",
  danger: "#C44747",
  surface: "#FFFFFF",
  muted: "#F3F5F7",
  border: "#D7DEE7",
  ink: "#1C2430",
  inkMuted: "#5B6573",
  white: "#FFFFFF",
} as const;

export type ColorName = keyof typeof colors;
