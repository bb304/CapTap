import React from "react";
import { StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Screen, SectionHeader } from "@/components/ui";
import { FormTextField } from "@/components/forms/FormTextField";
import { FormError } from "@/components/forms/FormError";
import {
  forgotPasswordSchema,
  type ForgotPasswordFormValues,
} from "@/components/forms/schemas";
import { useForgotPassword } from "@/hooks/useAuthMutations";
import { spacing, typography } from "@/theme";

export default function ForgotPasswordScreen() {
  const router = useRouter();
  const forgotPassword = useForgotPassword();
  const { control, handleSubmit } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: "" },
  });

  const onSubmit = handleSubmit((values) => {
    forgotPassword.mutate(values);
  });

  return (
    <Screen>
      <SectionHeader
        title="Reset password"
        subtitle="We'll email a reset link if the account exists"
      />
      <View style={styles.form}>
        {forgotPassword.isSuccess ? (
          <Text style={styles.notice} maxFontSizeMultiplier={1.5}>
            If an account exists for that email, a reset link is on its way. Check
            your inbox.
          </Text>
        ) : (
          <>
            <FormTextField
              control={control}
              name="email"
              label="Email"
              autoCapitalize="none"
              keyboardType="email-address"
              placeholder="you@example.com"
            />
            <FormError
              error={forgotPassword.isError ? forgotPassword.error : undefined}
            />
            <Button
              label="Send reset link"
              onPress={onSubmit}
              loading={forgotPassword.isPending}
            />
          </>
        )}
        <Button label="Back to login" variant="ghost" onPress={() => router.back()} />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  form: { gap: spacing.lg },
  notice: { ...typography.body },
});
