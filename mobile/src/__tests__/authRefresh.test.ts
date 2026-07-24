import { describe, expect, it, jest, afterEach, beforeEach } from "@jest/globals";
import MockAdapter from "axios-mock-adapter";

// Mock the session so we control refresh behavior deterministically.
jest.mock("@/api/session", () => ({
  session: {
    getAccessToken: jest.fn(() => "old-access"),
    refresh: jest.fn(),
  },
}));

import { apiClient } from "@/api/client";
import { medicationApi } from "@/api/medication";
import { ApiClientError } from "@/api/errors";
import { session } from "@/api/session";

const refreshMock = session.refresh as jest.MockedFunction<typeof session.refresh>;
const getAccessMock = session.getAccessToken as jest.MockedFunction<typeof session.getAccessToken>;

let mock: MockAdapter;

beforeEach(() => {
  mock = new MockAdapter(apiClient);
  refreshMock.mockReset();
  getAccessMock.mockReturnValue("old-access");
});

afterEach(() => {
  mock.restore();
});

describe("401 handling", () => {
  it("refreshes the token and retries the original request once", async () => {
    // A successful refresh rotates the stored access token, which the request
    // interceptor then attaches to the retried request.
    refreshMock.mockImplementation(async () => {
      getAccessMock.mockReturnValue("new-access");
      return "new-access";
    });

    mock
      .onGet("/api/v1/medications")
      .replyOnce(401, {
        success: false,
        error: { code: "UNAUTHORIZED", message: "Access token expired." },
      })
      .onGet("/api/v1/medications")
      .replyOnce(200, { success: true, data: [] });

    const result = await medicationApi.list();

    expect(result).toEqual([]);
    expect(refreshMock).toHaveBeenCalledTimes(1);
    // The retried request carries the refreshed token.
    expect(mock.history.get).toHaveLength(2);
    expect(mock.history.get[1].headers?.Authorization).toBe("Bearer new-access");
  });

  it("surfaces an unauthorized error when refresh fails", async () => {
    refreshMock.mockResolvedValue(null);

    mock.onGet("/api/v1/medications").reply(401, {
      success: false,
      error: { code: "UNAUTHORIZED", message: "Access token expired." },
    });

    await expect(medicationApi.list()).rejects.toBeInstanceOf(ApiClientError);
    expect(refreshMock).toHaveBeenCalledTimes(1);
  });
});
