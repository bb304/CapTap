import { describe, expect, it, afterEach, beforeEach } from "@jest/globals";
import MockAdapter from "axios-mock-adapter";
import { apiClient } from "@/api/client";
import { authApi } from "@/api/auth";
import { ApiClientError } from "@/api/errors";
import { session } from "@/api/session";

let mock: MockAdapter;

beforeEach(async () => {
  mock = new MockAdapter(apiClient);
  await session.clear();
});

afterEach(() => {
  mock.restore();
});

describe("authApi.login", () => {
  it("returns tokens on success", async () => {
    mock.onPost("/api/v1/auth/login").reply(200, {
      success: true,
      data: { accessToken: "a", refreshToken: "r", expiresIn: 900 },
    });

    const tokens = await authApi.login({ email: "a@b.com", password: "pw" });
    expect(tokens.accessToken).toBe("a");
    expect(tokens.refreshToken).toBe("r");
  });

  it("throws a typed error on invalid credentials", async () => {
    mock.onPost("/api/v1/auth/login").reply(401, {
      success: false,
      error: { code: "UNAUTHORIZED", message: "Invalid email or password." },
    });

    await expect(
      authApi.login({ email: "a@b.com", password: "wrong" }),
    ).rejects.toMatchObject({
      name: "ApiClientError",
      kind: "unauthorized",
      message: "Invalid email or password.",
    });
  });

  it("maps a network failure to a network error", async () => {
    mock.onPost("/api/v1/auth/login").networkError();

    await expect(
      authApi.login({ email: "a@b.com", password: "pw" }),
    ).rejects.toBeInstanceOf(ApiClientError);
  });

  it("does not attempt refresh on a 401 from an auth route", async () => {
    mock.onPost("/api/v1/auth/login").reply(401, {
      success: false,
      error: { code: "UNAUTHORIZED", message: "Invalid email or password." },
    });

    await expect(
      authApi.login({ email: "a@b.com", password: "pw" }),
    ).rejects.toBeInstanceOf(ApiClientError);
    // Only the single login attempt — no refresh/retry loop.
    expect(mock.history.post.filter((r) => r.url?.includes("/auth/login"))).toHaveLength(1);
  });
});
