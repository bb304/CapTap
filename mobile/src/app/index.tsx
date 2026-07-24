import { Redirect } from "expo-router";
import { Screen, LoadingSpinner } from "@/components/ui";
import { useAuth } from "@/context/AuthContext";
import { routes } from "@/constants/routes";

export default function Index() {
  const { status } = useAuth();

  if (status === "restoring") {
    return (
      <Screen scroll={false}>
        <LoadingSpinner label="Loading CapTap" />
      </Screen>
    );
  }

  return <Redirect href={status === "authenticated" ? routes.tabs : routes.welcome} />;
}
