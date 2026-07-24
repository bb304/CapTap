import React, { useState } from "react";
import { Alert, Pressable, StyleSheet, Switch, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { Button, Card, LoadingSpinner, Screen, SectionHeader } from "@/components/ui";
import { ErrorState } from "@/components/common/ErrorState";
import {
  useNotificationPreferences,
  useUpdateNotificationPreferences,
} from "@/hooks/useNotificationPreferences";
import { NotificationService } from "@/services/NotificationService";
import type { NotificationPreferences } from "@/types/notifications";
import { colors, spacing, touchTarget, typography } from "@/theme";

const OFFSET_OPTIONS = [
  { label: "30 minutes after", minutes: 30 },
  { label: "1 hour after (default)", minutes: 60 },
  { label: "2 hours after", minutes: 120 },
] as const;

const QUIET_WINDOW_OPTIONS = [
  { label: "10 PM – 7 AM (default)", start: "22:00", end: "07:00" },
  { label: "9 PM – 8 AM", start: "21:00", end: "08:00" },
  { label: "11 PM – 6 AM", start: "23:00", end: "06:00" },
] as const;

export default function NotificationSettingsScreen() {
  const router = useRouter();
  const prefsQuery = useNotificationPreferences();
  const update = useUpdateNotificationPreferences();
  const [permissionHint, setPermissionHint] = useState<string | null>(null);

  if (prefsQuery.isLoading) {
    return (
      <Screen>
        <LoadingSpinner label="Loading notification settings" />
      </Screen>
    );
  }

  if (prefsQuery.isError || !prefsQuery.data) {
    return (
      <Screen scroll={false}>
        <ErrorState error={prefsQuery.error} onRetry={() => prefsQuery.refetch()} />
        <Button label="Back" variant="ghost" onPress={() => router.back()} />
      </Screen>
    );
  }

  const prefs = prefsQuery.data;

  const apply = async (next: NotificationPreferences) => {
    try {
      const result = await update.mutateAsync(next);
      if (next.enabled && !result.permission.granted) {
        setPermissionHint(
          result.permission.message ??
            "Notifications are disabled for CapTap. Enable them in system Settings.",
        );
        Alert.alert(
          "Notifications unavailable",
          result.permission.message ??
            "Enable notifications in system Settings to receive reminders.",
        );
      } else {
        setPermissionHint(null);
      }
    } catch {
      Alert.alert(
        "Couldn't update reminders",
        "Check notification permissions and try again. CapTap will retry when you reopen the app.",
      );
    }
  };

  return (
    <Screen>
      <SectionHeader
        title="Reminders"
        subtitle="Helpful nudges — never noisy. Default: one hour after each dose."
      />

      <Card style={styles.card}>
        <View style={styles.row}>
          <View style={styles.copy}>
            <Text style={styles.title} maxFontSizeMultiplier={1.4}>
              Medication reminders
            </Text>
            <Text style={styles.hint} maxFontSizeMultiplier={1.4}>
              Local alerts after a scheduled dose if you haven't logged yet
            </Text>
          </View>
          <Switch
            value={prefs.enabled}
            onValueChange={(enabled) => apply({ ...prefs, enabled })}
            accessibilityLabel="Enable medication reminders"
          />
        </View>
      </Card>

      {permissionHint ? (
        <Card>
          <Text style={styles.warning} maxFontSizeMultiplier={1.4}>
            {permissionHint}
          </Text>
          <Button
            label="Check permission"
            variant="secondary"
            onPress={async () => {
              const state = await NotificationService.requestPermissions();
              setPermissionHint(state.message ?? null);
              if (state.granted) {
                await apply({ ...prefs, enabled: true });
              }
            }}
          />
        </Card>
      ) : null}

      <SectionHeader title="Reminder timing" />
      <Card style={styles.list}>
        {OFFSET_OPTIONS.map((option, index) => {
          const selected = prefs.reminderOffsetMinutes === option.minutes;
          return (
            <Pressable
              key={option.minutes}
              onPress={() => apply({ ...prefs, reminderOffsetMinutes: option.minutes })}
              accessibilityRole="button"
              accessibilityState={{ selected }}
              accessibilityLabel={option.label}
              style={[styles.option, index < OFFSET_OPTIONS.length - 1 ? styles.divider : null]}
            >
              <Text style={styles.title} maxFontSizeMultiplier={1.4}>
                {option.label}
              </Text>
              <Text style={styles.hint} maxFontSizeMultiplier={1.4}>
                {selected ? "Selected" : "Tap to select"}
              </Text>
            </Pressable>
          );
        })}
      </Card>

      <SectionHeader
        title="Quiet hours"
        subtitle="Reminders that would fire overnight are delayed until morning."
      />
      <Card style={styles.card}>
        <View style={styles.row}>
          <View style={styles.copy}>
            <Text style={styles.title} maxFontSizeMultiplier={1.4}>
              Quiet hours
            </Text>
            <Text style={styles.hint} maxFontSizeMultiplier={1.4}>
              {prefs.quietHoursEnabled
                ? `${prefs.quietHoursStart} – ${prefs.quietHoursEnd}`
                : "Off — reminders may fire overnight"}
            </Text>
          </View>
          <Switch
            value={prefs.quietHoursEnabled}
            onValueChange={(quietHoursEnabled) => apply({ ...prefs, quietHoursEnabled })}
            accessibilityLabel="Enable quiet hours"
          />
        </View>
      </Card>

      {prefs.quietHoursEnabled ? (
        <Card style={styles.list}>
          {QUIET_WINDOW_OPTIONS.map((option, index) => {
            const selected =
              prefs.quietHoursStart === option.start && prefs.quietHoursEnd === option.end;
            return (
              <Pressable
                key={option.label}
                onPress={() =>
                  apply({
                    ...prefs,
                    quietHoursStart: option.start,
                    quietHoursEnd: option.end,
                  })
                }
                accessibilityRole="button"
                accessibilityState={{ selected }}
                accessibilityLabel={option.label}
                style={[
                  styles.option,
                  index < QUIET_WINDOW_OPTIONS.length - 1 ? styles.divider : null,
                ]}
              >
                <Text style={styles.title} maxFontSizeMultiplier={1.4}>
                  {option.label}
                </Text>
                <Text style={styles.hint} maxFontSizeMultiplier={1.4}>
                  {selected ? "Selected" : "Tap to select"}
                </Text>
              </Pressable>
            );
          })}
        </Card>
      ) : null}

      <Button label="Back" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: { marginBottom: spacing.md },
  list: { paddingVertical: 0, paddingHorizontal: 0, overflow: "hidden" },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    minHeight: touchTarget,
  },
  copy: { flex: 1, gap: 2 },
  title: { ...typography.label, fontSize: 17 },
  hint: { ...typography.caption },
  warning: { ...typography.bodyMuted, color: colors.danger, marginBottom: spacing.sm },
  option: {
    minHeight: touchTarget + 8,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
  },
  divider: {
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
});
