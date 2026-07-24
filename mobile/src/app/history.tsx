import React from "react";
import { RefreshControl, StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { Button, Card, EmptyState, Screen, SectionHeader, SkeletonCard } from "@/components/ui";
import { ErrorState } from "@/components/common/ErrorState";
import { useMedicationLogHistory } from "@/hooks/useMedicationLogs";
import { usePullToRefresh } from "@/hooks/usePullToRefresh";
import { formatTime } from "@/utils/format";
import { colors, spacing, typography } from "@/theme";

export default function HistoryScreen() {
  const router = useRouter();
  const { data, isLoading, isError, error, refetch } = useMedicationLogHistory({
    page: 1,
    pageSize: 50,
  });
  const { refreshing, onRefresh } = usePullToRefresh(refetch);

  if (isLoading) {
    return (
      <Screen>
        <SectionHeader title="History" subtitle="Your recent dose logs" />
        <SkeletonCard />
        <SkeletonCard />
      </Screen>
    );
  }

  if (isError) {
    return (
      <Screen scroll={false}>
        <ErrorState error={error} onRetry={() => refetch()} />
        <Button label="Back" variant="ghost" onPress={() => router.back()} />
      </Screen>
    );
  }

  const items = data?.items ?? [];

  return (
    <Screen
      refreshControl={
        <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={colors.primary} />
      }
    >
      <SectionHeader title="History" subtitle="Newest first" />
      {items.length === 0 ? (
        <EmptyState
          title="No logs yet"
          description="When you mark a dose as taken, it will show up here."
        />
      ) : (
        items.map((item) => (
          <Card key={item.id} style={styles.card}>
            <Text style={styles.name} maxFontSizeMultiplier={1.5}>
              {item.medicationName}
            </Text>
            <Text style={styles.meta} maxFontSizeMultiplier={1.5}>
              Scheduled {formatTime(item.scheduledDoseTime)} · Logged{" "}
              {new Date(item.loggedAt).toLocaleString()}
            </Text>
            <View style={styles.method}>
              <Text style={styles.methodText} maxFontSizeMultiplier={1.4}>
                {item.loggingMethod}
              </Text>
            </View>
          </Card>
        ))
      )}
      <Button label="Back" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: { gap: spacing.xs },
  name: { ...typography.subtitle, fontSize: 20 },
  meta: { ...typography.bodyMuted, fontSize: 15 },
  method: {
    alignSelf: "flex-start",
    marginTop: spacing.xs,
    backgroundColor: colors.primarySoft,
    borderRadius: 999,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
  },
  methodText: { ...typography.caption, color: colors.primaryDark, fontWeight: "700" },
});
