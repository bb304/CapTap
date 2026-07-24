/** Centralized API paths. Never hardcode URLs elsewhere. */
export const endpoints = {
  auth: {
    login: "/api/v1/auth/login",
    register: "/api/v1/auth/register",
    refresh: "/api/v1/auth/refresh",
    logout: "/api/v1/auth/logout",
    forgotPassword: "/api/v1/auth/forgot-password",
    resetPassword: "/api/v1/auth/reset-password",
    verifyEmail: "/api/v1/auth/verify-email",
  },
  medications: {
    list: "/api/v1/medications",
    search: "/api/v1/medications/search",
    byId: (id: string) => `/api/v1/medications/${id}`,
    create: "/api/v1/medications",
    update: (id: string) => `/api/v1/medications/${id}`,
    archive: (id: string) => `/api/v1/medications/${id}/archive`,
    schedules: (id: string) => `/api/v1/medications/${id}/schedules`,
  },
  schedules: {
    update: (id: string) => `/api/v1/schedules/${id}`,
    remove: (id: string) => `/api/v1/schedules/${id}`,
  },
  dashboard: {
    today: "/api/v1/dashboard/today",
    missed: "/api/v1/dashboard/missed",
    streak: "/api/v1/dashboard/streak",
  },
  medicationLogs: {
    create: "/api/v1/medication-logs",
    history: "/api/v1/medication-logs/history",
  },
  nfc: {
    tags: "/api/v1/nfc/tags",
    assign: "/api/v1/nfc/assign",
    unassign: "/api/v1/nfc/unassign",
    resolve: (tagIdentifier: string) => `/api/v1/nfc/${tagIdentifier}`,
  },
  users: {
    me: "/api/v1/users/me",
    timezone: "/api/v1/users/me/timezone",
    deleteMe: "/api/v1/users/me",
  },
} as const;
