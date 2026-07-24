import { describe, expect, it, jest, beforeEach, afterEach } from "@jest/globals";

jest.mock("@/api/session", () => ({
  session: {
    getAccessToken: jest.fn(),
    refresh: jest.fn(),
  },
}));

import { session } from "@/api/session";
import {
  scheduleProactiveRefresh,
  cancelProactiveRefresh,
} from "@/api/tokenRefresh";

const getAccessMock = session.getAccessToken as jest.MockedFunction<
  typeof session.getAccessToken
>;
const refreshMock = session.refresh as jest.MockedFunction<typeof session.refresh>;

/** Build an unsigned JWT with a near-future exp. */
function buildJwt(expSecondsFromNow: number): string {
  const exp = Math.floor(Date.now() / 1000) + expSecondsFromNow;
  const encode = (obj: Record<string, unknown>) =>
    Buffer.from(JSON.stringify(obj)).toString("base64url");
  return `${encode({ alg: "none" })}.${encode({ userId: "u1", email: "a@b.com", exp })}.sig`;
}

beforeEach(() => {
  jest.useFakeTimers();
  getAccessMock.mockReset();
  refreshMock.mockReset();
  cancelProactiveRefresh();
});

afterEach(() => {
  cancelProactiveRefresh();
  jest.useRealTimers();
});

describe("proactive token refresh", () => {
  it("refreshes shortly before JWT expiration", async () => {
    // Token expires in 90s → refresh scheduled at ~30s (90 - 60 skew).
    const nearExpiry = buildJwt(90);
    const farExpiry = buildJwt(3600);
    getAccessMock.mockReturnValue(nearExpiry);
    refreshMock.mockImplementation(async () => {
      getAccessMock.mockReturnValue(farExpiry);
      return "new-access";
    });

    scheduleProactiveRefresh();

    expect(refreshMock).not.toHaveBeenCalled();
    await jest.advanceTimersByTimeAsync(31_000);
    expect(refreshMock).toHaveBeenCalledTimes(1);
  });

  it("does nothing when there is no access token", () => {
    getAccessMock.mockReturnValue(null);
    scheduleProactiveRefresh();
    jest.advanceTimersByTime(120_000);
    expect(refreshMock).not.toHaveBeenCalled();
  });
});
