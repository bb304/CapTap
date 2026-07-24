const { getDefaultConfig } = require("expo/metro-config");
const { withNativeWind } = require("nativewind/metro");
const exclusionList = require("metro-config/private/defaults/exclusionList").default;

const config = getDefaultConfig(__dirname);

// CapTap lives on iCloud Desktop — ignore native/build trees so File Provider
// churn doesn't spam Expo Fast Refresh ("Refreshing..." banner).
config.resolver.blockList = exclusionList([
  /\/\.git\/.*/,
  /\/ios\/Pods\/.*/,
  /\/ios\/build\/.*/,
  /\/android\/\.gradle\/.*/,
  /\/android\/build\/.*/,
  /\/android\/app\/build\/.*/,
  /\/coverage\/.*/,
  /\/node_modules\/.*\/android\/.*/,
  /\/node_modules\/.*\/ios\/.*/,
  /\/node_modules\/expo-modules-jsi\/apple\/(Products|\.DerivedData|\.build|\.swiftpm|\.generated)\/.*/,
]);

module.exports = withNativeWind(config, { input: "./src/global.css" });
