import React from "react";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Screen, SectionHeader } from "@/components/ui";
import { FormTextField } from "@/components/forms/FormTextField";
import { FormError } from "@/components/forms/FormError";
import { loginSchema, type LoginFormValues } from "@/components/forms/schemas";
import { useLogin } from "@/hooks/useAuthMutations";
import { routes } from "@/constants/routes";
import { colors, spacing, touchTarget, typography } from "@/theme";

export default function LoginScreen() {
  const router = useRouter();
  const login = useLogin();
  const { control, handleSubmit } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  const onSubmit = handleSubmit((values) => {
    login.mutate(values, {
      onSuccess: () => router.replace(routes.tabs),
    });
  });

  return (
    <Screen>
      <SectionHeader title="Welcome back" subtitle="Log in to CapTap" />
      <View style={styles.form}>
        <FormTextField
          control={control}
          name="email"
          label="Email"
          autoCapitalize="none"
          keyboardType="email-address"
          placeholder="you@example.com"
        />
        <FormTextField
          control={control}
          name="password"
          label="Password"
          secureTextEntry
          placeholder="Your password"
        />
        <Pressable
          onPress={() => router.push(routes.forgotPassword)}
          accessibilityRole="button"
          accessibilityLabel="Forgot password"
          style={styles.forgot}
        >
          <Text style={styles.forgotText} maxFontSizeMultiplier={1.4}>
            Forgot password?
          </Text>
        </Pressable>

        <FormError error={login.isError ? login.error : undefined} />

        <Button label="Log in" onPress={onSubmit} loading={login.isPending} />
        <Button label="Back" variant="ghost" onPress={() => router.back()} />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  form: { gap: spacing.lg },
  forgot: {
    minHeight: touchTarget,
    justifyContent: "center",
    alignSelf: "flex-start",
  },
  forgotText: {
    ...typography.label,
    color: colors.primaryDark,
  },
});
