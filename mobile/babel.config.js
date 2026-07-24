module.exports = function (api) {
  const isTest = process.env.NODE_ENV === "test" || !!process.env.JEST_WORKER_ID;
  api.cache(true);

  return {
    presets: [
      [
        "babel-preset-expo",
        isTest ? {} : { jsxImportSource: "nativewind" },
      ],
      ...(isTest ? [] : ["nativewind/babel"]),
    ],
    plugins: ["react-native-reanimated/plugin"],
  };
};
