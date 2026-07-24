import "react-native-gesture-handler";

process.env.EXPO_PUBLIC_API_URL =
  process.env.EXPO_PUBLIC_API_URL ?? "http://localhost:5001";

jest.mock("expo-router", () => {
  const React = require("react");
  return {
    useRouter: () => ({
      push: jest.fn(),
      replace: jest.fn(),
      back: jest.fn(),
    }),
    useLocalSearchParams: () => ({}),
    Link: ({ children }: { children: React.ReactNode }) => children,
    Stack: { Screen: () => null },
    Tabs: { Screen: () => null },
    Redirect: () => null,
  };
});

// In-memory SecureStore so session/token tests are deterministic.
jest.mock("expo-secure-store", () => {
  const store = new Map<string, string>();
  return {
    setItemAsync: jest.fn(async (key: string, value: string) => {
      store.set(key, value);
    }),
    getItemAsync: jest.fn(async (key: string) => store.get(key) ?? null),
    deleteItemAsync: jest.fn(async (key: string) => {
      store.delete(key);
    }),
    __store: store,
  };
});

jest.mock("react-native-reanimated", () => {
  const Reanimated = require("react-native-reanimated/mock");
  Reanimated.default.call = () => undefined;
  return Reanimated;
});
