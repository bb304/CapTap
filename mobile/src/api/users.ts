/** User profile / account endpoints. */
import { apiClient, unwrap } from "./client";
import { endpoints } from "./endpoints";

export type UserProfile = {
  id: string;
  email: string;
  timeZoneId: string;
};

export const usersApi = {
  me: async (): Promise<UserProfile> => {
    const { data } = await apiClient.get(endpoints.users.me);
    return unwrap<UserProfile>(data);
  },

  /** Soft-delete + anonymize. Requires current password. Returns 204. */
  deleteAccount: async (password: string): Promise<void> => {
    await apiClient.delete(endpoints.users.deleteMe, {
      data: { password },
    });
  },
};
