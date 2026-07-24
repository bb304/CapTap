import React, { useEffect } from "react";
import { StyleSheet, Text, View } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { Button, LoadingSpinner, Screen, SectionHeader } from "@/components/ui";
import { FormError } from "@/components/forms/FormError";
import { useVerifyEmail } from "@/hooks/useAuthMutations";
import { routes } from "@/constants/routes";
import { spacing, typography } from "@/theme";

export default function VerifyEmailScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ token?: string }>();
  const token = typeof params.token === "string" ? params.token : "";
  const verify = useVerifyEmail();

  useEffect(() => {
    if (token) {
      verify.mutate(token);
    }
    // Run once when token is present.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  return (
    <Screen>
      <SectionHeader title="Verify email" subtitle="Confirming your CapTap account" />
      <View style={styles.body}>
        {!token ? (
          <Text style={styles.copy} maxFontSizeMultiplier={1.5}>
            Open the verification link from your CapTap email to finish confirming your address.
          </Text>
        ) : verify.isPending ? (
          <LoadingSpinner label="Verifying email" />
        ) : verify.isSuccess ? (
          <Text style={styles.copy} maxFontSizeMultiplier={1.5}>
            Your email is verified. You can sign in.
          </Text>
        ) : (
          <FormError error={verify.isError ? verify.error : undefined} />
        )}
        <Button label="Go to login" onPress={() => router.replace(routes.login)} />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  body: { gap: spacing.lg },
  copy: { ...typography.body },
});
