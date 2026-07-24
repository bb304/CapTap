const React = require("react");
const { View } = require("react-native");

function Icon(props) {
  return React.createElement(View, { ...props, accessibilityRole: "image" });
}

module.exports = {
  __esModule: true,
  default: Icon,
  Search: Icon,
  X: Icon,
  Plus: Icon,
  Home: Icon,
  Pill: Icon,
  Settings: Icon,
  ChevronRight: Icon,
  Nfc: Icon,
  WifiOff: Icon,
  AlertCircle: Icon,
  Inbox: Icon,
};
