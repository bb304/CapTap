import { TextStyle } from "react-native";
import { colors } from "./colors";

export const typography = {
  hero: {
    fontSize: 40,
    lineHeight: 46,
    fontWeight: "700",
    color: colors.ink,
  } satisfies TextStyle,
  title: {
    fontSize: 28,
    lineHeight: 34,
    fontWeight: "700",
    color: colors.ink,
  } satisfies TextStyle,
  subtitle: {
    fontSize: 20,
    lineHeight: 28,
    fontWeight: "600",
    color: colors.ink,
  } satisfies TextStyle,
  body: {
    fontSize: 18,
    lineHeight: 26,
    fontWeight: "400",
    color: colors.ink,
  } satisfies TextStyle,
  bodyMuted: {
    fontSize: 18,
    lineHeight: 26,
    fontWeight: "400",
    color: colors.inkMuted,
  } satisfies TextStyle,
  label: {
    fontSize: 16,
    lineHeight: 22,
    fontWeight: "600",
    color: colors.ink,
  } satisfies TextStyle,
  caption: {
    fontSize: 15,
    lineHeight: 20,
    fontWeight: "500",
    color: colors.inkMuted,
  } satisfies TextStyle,
} as const;
