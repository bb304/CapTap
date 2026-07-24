import { describe, expect, it, jest, beforeEach } from "@jest/globals";
import MockAdapter from "axios-mock-adapter";
import { session, refreshClient } from "@/api/session";
import type { AuthTokens } from "@/api/types";

const tokens: AuthTokens = {
  accessToken: "access-1",
  refreshToken: "refresh-1",
  expiresIn: 900,
};

describe("session store", () => {
  beforeEach(async () => {
    await session.clear();
  });

  it("persists and restores tokens via SecureStore", async () => {
    await session.setTokens(tokens);
    expect(session.getAccessToken()).toBe("access-1");
    expect(session.getRefreshToken()).toBe("refresh-1");
    expect(session.hasSession()).toBe(true);

    const restored = await session.restore();
    expect(restored).toBe(true);
    expect(session.getAccessToken()).toBe("access-1");
  });

  it("clears tokens", async () => {
    await session.setTokens(tokens);
    await session.clear();
    expect(session.getAccessToken()).toBeNull();
    expect(session.hasSession()).toBe(false);
  });

  it("refreshes the token pair (single-flight)", async () => {
    await session.setTokens(tokens);
    const mock = new MockAdapter(refreshClient);
    mock.onPost("/api/v1/auth/refresh").reply(200, {
      success: true,
      data: { accessToken: "access-2", refreshToken: "refresh-2", expiresIn: 900 },
    });

    const [a, b] = await Promise.all([session.refresh(), session.refresh()]);
    expect(a).toBe("access-2");
    expect(b).toBe("access-2");
    // Single-flight: only one network call despite two callers.
    expect(mock.history.post.length).toBe(1);
    expect(session.getRefreshToken()).toBe("refresh-2");
    mock.restore();
  });

  it("expires the session and notifies listeners when refresh fails", async () => {
    await session.setTokens(tokens);
    const mock = new MockAdapter(refreshClient);
    mock.onPost("/api/v1/auth/refresh").reply(401, {
      success: false,
      error: { code: "UNAUTHORIZED", message: "Invalid refresh token." },
    });

    const onExpire = jest.fn();
    const unsubscribe = session.onExpire(onExpire);

    const result = await session.refresh();
    expect(result).toBeNull();
    expect(onExpire).toHaveBeenCalledTimes(1);
    expect(session.hasSession()).toBe(false);

    unsubscribe();
    mock.restore();
  });
});
