import React, { useState } from "react";
import { Alert, RefreshControl, StyleSheet, View } from "react-native";
import { useRouter } from "expo-router";
import { DoseCard } from "@/components/dashboard/DoseCard";
import { StatCard } from "@/components/dashboard/StatCard";
import { OfflineSyncBanner } from "@/components/common/OfflineSyncBanner";
import { Button, EmptyState, Screen, SectionHeader, SkeletonCard } from "@/components/ui";
import { ErrorState } from "@/components/common/ErrorState";
import { useDashboard } from "@/hooks/useDashboard";
import { useLogMedication } from "@/hooks/useMedicationLogs";
import { usePullToRefresh } from "@/hooks/usePullToRefresh";
import { toUserMessage } from "@/api/errors";
import type { TodayDose } from "@/types/dashboard";
import { toScheduledDoseIso } from "@/utils/format";
import { routes } from "@/constants/routes";
import { colors, spacing } from "@/theme";

export default function DashboardScreen() {
  const router = useRouter();
  const { data, isLoading, isError, error, refetch } = useDashboard();
  const { refreshing, onRefresh } = usePullToRefresh(refetch);
  const logMedication = useLogMedication();
  const [pendingDoseId, setPendingDoseId] = useState<string | null>(null);

  const onMarkTaken = (dose: TodayDose) => {
    setPendingDoseId(dose.id);
    logMedication.mutate(
      {
        medicationId: dose.medicationId,
        scheduleId: dose.scheduleId,
        scheduledDoseTime: dose.scheduledDoseTime ?? toScheduledDoseIso(dose.scheduledTime),
        loggingMethod: "Manual",
      },
      {
        onError: (err) => Alert.alert("Couldn't log dose", toUserMessage(err)),
        onSettled: () => setPendingDoseId(null),
      },
    );
  };

  if (isLoading) {
    return (
      <Screen>
        <SectionHeader title="Today" subtitle="Did I take my medication today?" />
        <View style={styles.stats}>
          <SkeletonCard />
          <SkeletonCard />
        </View>
        <SkeletonCard />
        <SkeletonCard />
      </Screen>
    );
  }

  if (isError || !data) {
    return (
      <Screen scroll={false}>
        <ErrorState error={error} onRetry={() => refetch()} />
      </Screen>
    );
  }

  return (
    <Screen
      refreshControl={
        <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={colors.primary} />
      }
    >
      <SectionHeader title="Today" subtitle="Did I take my medication today?" />

      <OfflineSyncBanner />

      <Button
        label="Scan bottle tag"
        variant="secondary"
        onPress={() => router.push(routes.nfcScan)}
      />

      <View style={styles.stats}>
        <StatCard
          label="Current streak"
          value={`${data.currentStreakDays}d`}
          hint="Keep it going"
        />
        <StatCard
          label="Longest streak"
          value={`${data.longestStreakDays}d`}
          hint="Personal best"
        />
        <StatCard
          label="Today"
          value={`${data.completionPercent}%`}
          hint={`${data.takenCount}/${data.totalMedicationsToday} taken`}
        />
      </View>

      <SectionHeader title="Today's medications" />
      {data.today.length === 0 ? (
        <EmptyState
          title="Nothing scheduled"
          description="Add a medication to see today's doses."
        />
      ) : (
        data.today.map((dose) => (
          <DoseCard
            key={dose.id}
            dose={dose}
            onMarkTaken={onMarkTaken}
            marking={pendingDoseId === dose.id && logMedication.isPending}
          />
        ))
      )}

      <SectionHeader title="Upcoming" />
      {data.upcoming.length === 0 ? (
        <EmptyState title="No upcoming doses" description="You're all caught up for now." />
      ) : (
        data.upcoming.map((dose) => (
          <DoseCard
            key={`up-${dose.id}`}
            dose={dose}
            onMarkTaken={onMarkTaken}
            marking={pendingDoseId === dose.id && logMedication.isPending}
          />
        ))
      )}

      <SectionHeader title="Missed" subtitle="From recent days" />
      {data.missed.length === 0 ? (
        <EmptyState title="No missed doses" description="Nice work staying on track." />
      ) : (
        data.missed.map((dose) => <DoseCard key={`miss-${dose.id}`} dose={dose} />)
      )}
    </Screen>
  );
}

const styles = StyleSheet.create({
  stats: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: spacing.md,
  },
});
