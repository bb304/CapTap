/** Authentication API calls. No React, no UI — pure networking. */
import { apiClient, unwrap } from "./client";
import { endpoints } from "./endpoints";
import type {
  ApiResponse,
  AuthTokens,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  RegisterResponse,
  ResetPasswordRequest,
  VerifyEmailRequest,
} from "./types";

export const authApi = {
  async login(request: LoginRequest): Promise<AuthTokens> {
    const response = await apiClient.post<ApiResponse<AuthTokens>>(endpoints.auth.login, request);
    return unwrap(response);
  },

  async register(request: RegisterRequest): Promise<RegisterResponse> {
    const response = await apiClient.post<ApiResponse<RegisterResponse>>(
      endpoints.auth.register,
      request,
    );
    return unwrap(response);
  },

  async logout(refreshToken: string): Promise<void> {
    await apiClient.post(endpoints.auth.logout, { refreshToken });
  },

  async forgotPassword(request: ForgotPasswordRequest): Promise<void> {
    await apiClient.post(endpoints.auth.forgotPassword, request);
  },

  async resetPassword(request: ResetPasswordRequest): Promise<void> {
    await apiClient.post(endpoints.auth.resetPassword, request);
  },

  async verifyEmail(request: VerifyEmailRequest): Promise<void> {
    await apiClient.post(endpoints.auth.verifyEmail, request);
  },
};
