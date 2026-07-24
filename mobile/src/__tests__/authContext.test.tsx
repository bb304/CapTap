import { describe, expect, it, jest, beforeEach } from "@jest/globals";
import React from "react";
import { renderHook, waitFor, act } from "@testing-library/react-native";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import * as SecureStore from "expo-secure-store";

jest.mock("@/api/auth", () => ({
  authApi: {
    login: jest.fn(),
    register: jest.fn(),
    logout: jest.fn(),
  },
}));

import { authApi } from "@/api/auth";
import { AuthProvider, useAuth } from "@/context/AuthContext";
import { useLogin } from "@/hooks/useAuthMutations";
import { session } from "@/api/session";
import { ApiClientError } from "@/api/errors";

const loginMock = authApi.login as jest.MockedFunction<typeof authApi.login>;

/** Build an unsigned-but-decodable JWT for tests. */
function buildJwt(payload: Record<string, unknown>): string {
  const encode = (obj: Record<string, unknown>) =>
    Buffer.from(JSON.stringify(obj)).toString("base64url");
  return `${encode({ alg: "HS256", typ: "JWT" })}.${encode(payload)}.sig`;
}

const accessJwt = buildJwt({ userId: "u1", email: "a@b.com", exp: 9999999999 });

function createWrapper() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return (
      <QueryClientProvider client={client}>
        <AuthProvider>{children}</AuthProvider>
      </QueryClientProvider>
    );
  };
}

beforeEach(async () => {
  loginMock.mockReset();
  await session.clear();
});

describe("AuthContext", () => {
  it("restores a persisted session on launch", async () => {
    await SecureStore.setItemAsync("captap.accessToken", accessJwt);
    await SecureStore.setItemAsync("captap.refreshToken", "refresh-1");

    const { result } = renderHook(() => useAuth(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.status).toBe("authenticated"));
    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user?.email).toBe("a@b.com");
  });

  it("starts unauthenticated with no persisted session", async () => {
    const { result } = renderHook(() => useAuth(), { wrapper: createWrapper() });
    await waitFor(() => expect(result.current.status).toBe("unauthenticated"));
    expect(result.current.user).toBeNull();
  });

  it("signs in successfully and stores tokens", async () => {
    loginMock.mockResolvedValue({
      accessToken: accessJwt,
      refreshToken: "refresh-1",
      expiresIn: 900,
    });

    const { result } = renderHook(
      () => ({ auth: useAuth(), login: useLogin() }),
      { wrapper: createWrapper() },
    );
    await waitFor(() => expect(result.current.auth.status).toBe("unauthenticated"));

    await act(async () => {
      await result.current.login.mutateAsync({ email: "a@b.com", password: "pw" });
    });

    await waitFor(() => expect(result.current.auth.isAuthenticated).toBe(true));
    expect(result.current.auth.user?.email).toBe("a@b.com");
    expect(session.getAccessToken()).toBe(accessJwt);
  });

  it("surfaces a failed login without authenticating", async () => {
    loginMock.mockRejectedValue(
      new ApiClientError({
        kind: "unauthorized",
        message: "Invalid email or password.",
      }),
    );

    const { result } = renderHook(
      () => ({ auth: useAuth(), login: useLogin() }),
      { wrapper: createWrapper() },
    );
    await waitFor(() => expect(result.current.auth.status).toBe("unauthenticated"));

    await act(async () => {
      await result.current.login
        .mutateAsync({ email: "a@b.com", password: "wrong" })
        .catch(() => undefined);
    });

    await waitFor(() => expect(result.current.login.isError).toBe(true));
    expect(result.current.auth.isAuthenticated).toBe(false);
  });
});
