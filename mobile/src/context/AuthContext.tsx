/**
 * Authentication context — the app's single interface to auth state.
 *
 * Screens consume `user`, `status`, and the `signIn` / `signUp` / `signOut`
 * actions. Tokens are owned by the {@link session} store (SecureStore-backed)
 * and never exposed to UI components. A 401 that cannot be refreshed clears the
 * session here and flips the app back to the unauthenticated state.
 */
import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { authApi } from "@/api/auth";
import { session } from "@/api/session";
import type { AuthUser } from "@/types/auth";
import { clearLocalUserState } from "@/services/clearLocalUserState";
import { decodeTokenClaims } from "@/utils/jwt";
import { syncDeviceTimeZone } from "@/utils/timeZone";

export type AuthStatus = "restoring" | "authenticated" | "unauthenticated";

type AuthContextValue = {
  status: AuthStatus;
  isAuthenticated: boolean;
  user: AuthUser | null;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string, confirmPassword: string) => Promise<void>;
  signOut: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function userFromAccessToken(): AuthUser | null {
  const claims = decodeTokenClaims(session.getAccessToken());
  if (!claims?.userId) return null;
  return { id: claims.userId, email: claims.email ?? "" };
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<AuthStatus>("restoring");
  const [user, setUser] = useState<AuthUser | null>(null);
  const mounted = useRef(true);

  // Restore any persisted session on launch.
  useEffect(() => {
    mounted.current = true;
    (async () => {
      const restored = await session.restore();
      if (!mounted.current) return;
      if (restored) {
        setUser(userFromAccessToken());
        setStatus("authenticated");
        void syncDeviceTimeZone();
      } else {
        // No session — drop any leftover offline cache from a prior install/user.
        await clearLocalUserState(queryClient);
        if (!mounted.current) return;
        setStatus("unauthenticated");
      }
    })();
    return () => {
      mounted.current = false;
    };
  }, [queryClient]);

  // A failed refresh (expired/revoked session) drops us to Login.
  useEffect(() => {
    return session.onExpire(() => {
      void (async () => {
        await clearLocalUserState(queryClient);
        if (!mounted.current) return;
        setUser(null);
        setStatus("unauthenticated");
      })();
    });
  }, [queryClient]);

  const signIn = useCallback(
    async (email: string, password: string) => {
      // Clear prior account data before hydrating the new session.
      await clearLocalUserState(queryClient);
      const tokens = await authApi.login({
        email: email.trim().toLowerCase(),
        password,
      });
      await session.setTokens(tokens);
      setUser(userFromAccessToken());
      setStatus("authenticated");
      void syncDeviceTimeZone();
    },
    [queryClient],
  );

  const signUp = useCallback(
    async (email: string, password: string, confirmPassword: string) => {
      const normalizedEmail = email.trim().toLowerCase();
      await authApi.register({
        email: normalizedEmail,
        password,
        confirmPassword,
      });
      // Registration creates an active account; sign in immediately so the
      // user never sees an extra login step.
      await signIn(normalizedEmail, password);
    },
    [signIn],
  );

  const signOut = useCallback(async () => {
    const refreshToken = session.getRefreshToken();
    if (refreshToken) {
      // Best-effort server-side revocation; ignore network failures on logout.
      try {
        await authApi.logout(refreshToken);
      } catch {
        // no-op
      }
    }
    await session.clear();
    await clearLocalUserState(queryClient);
    setUser(null);
    setStatus("unauthenticated");
  }, [queryClient]);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      isAuthenticated: status === "authenticated",
      user,
      signIn,
      signUp,
      signOut,
    }),
    [status, user, signIn, signUp, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return context;
}
