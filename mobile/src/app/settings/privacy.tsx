import React, { useState } from "react";
import { Alert, StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { useQueryClient } from "@tanstack/react-query";
import { Button, Card, Input, Screen, SectionHeader } from "@/components/ui";
import { usersApi } from "@/api/users";
import { useAuth } from "@/context/AuthContext";
import { routes } from "@/constants/routes";
import { LocalDatabase } from "@/services/localDatabase";
import { NotificationService } from "@/services/NotificationService";
import { ApiClientError, toUserMessage } from "@/api/errors";
import { colors, spacing, typography } from "@/theme";

export default function PrivacySettingsScreen() {
  const router = useRouter();
  const { signOut } = useAuth();
  const queryClient = useQueryClient();
  const [password, setPassword] = useState("");
  const [deleting, setDeleting] = useState(false);

  const confirmDelete = () => {
    if (password.trim().length < 8) {
      Alert.alert("Password required", "Enter your current password to delete your account.");
      return;
    }

    Alert.alert(
      "Delete your account?",
      "CapTap will anonymize your email and sign-in details, revoke sessions, and unassign NFC tags. Medication log history is kept without your personal identity. This cannot be undone.",
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Delete account",
          style: "destructive",
          onPress: () => void performDelete(),
        },
      ],
    );
  };

  const performDelete = async () => {
    setDeleting(true);
    try {
      await usersApi.deleteAccount(password);
      await NotificationService.cancelAllCapTapReminders();
      await LocalDatabase.clearAllUserData();
      queryClient.clear();
      setPassword("");
      await signOut();
      router.replace(routes.welcome);
      Alert.alert(
        "Account deleted",
        "Your personal details were anonymized. You can create a new CapTap account anytime.",
      );
    } catch (error) {
      const message =
        error instanceof ApiClientError
          ? toUserMessage(error)
          : "Unable to delete your account right now. Try again when you have a connection.";
      Alert.alert("Couldn't delete account", message);
    } finally {
      setDeleting(false);
    }
  };

  return (
    <Screen>
      <SectionHeader
        title="Privacy"
        subtitle="CapTap collects only what you need to track your own medications."
      />

      <Card style={styles.card}>
        <Text style={styles.body} maxFontSizeMultiplier={1.5}>
          We store your email for sign-in, medication schedules you add, and dose logs you confirm.
          NFC stickers only hold a tag identifier — never medication names.
        </Text>
        <Text style={[styles.body, styles.gap]} maxFontSizeMultiplier={1.5}>
          Secrets stay in environment configuration or your device SecureStore. Passwords are hashed
          (Argon2id). Errors never include stack traces or database details.
        </Text>
      </Card>

      <SectionHeader
        title="Delete account"
        subtitle="Soft-delete and anonymize. Logs remain without your identity."
      />
      <Card style={styles.card}>
        <Text style={styles.body} maxFontSizeMultiplier={1.5}>
          Deleting signs you out everywhere, frees your email for a new account, and turns off NFC
          assignments. Confirm with your password — this is permanent.
        </Text>
        <View style={styles.gap}>
          <Input
            label="Current password"
            value={password}
            onChangeText={setPassword}
            secureTextEntry
            autoCapitalize="none"
            autoCorrect={false}
            textContentType="password"
            editable={!deleting}
          />
          <Button
            label={deleting ? "Deleting…" : "Delete my account"}
            variant="secondary"
            onPress={confirmDelete}
            disabled={deleting}
            accessibilityHint="Permanently anonymizes your CapTap account after password confirmation"
          />
        </View>
      </Card>

      <Button label="Back" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: { marginBottom: spacing.md, gap: spacing.sm },
  body: { ...typography.body, color: colors.ink, lineHeight: 24 },
  gap: { marginTop: spacing.md },
});
