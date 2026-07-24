import React from "react";
import { EmptyState } from "@/components/ui";

type NoResultsProps = {
  query?: string;
};

export function NoResults({ query }: NoResultsProps) {
  return (
    <EmptyState
      title="No results found"
      description={
        query
          ? `Nothing matched “${query}”. Try a different spelling.`
          : "Try a different search."
      }
    />
  );
}
