/**
 * Auth mutation hooks. These wrap the AuthContext actions (login / register)
 * and the password-reset API so screens get consistent loading + error state.
 */
import { useMutation } from "@tanstack/react-query";
import { authApi } from "@/api/auth";
import { useAuth } from "@/context/AuthContext";

export function useLogin() {
  const { signIn } = useAuth();
  return useMutation({
    mutationFn: (vars: { email: string; password: string }) => signIn(vars.email, vars.password),
  });
}

export function useRegister() {
  const { signUp } = useAuth();
  return useMutation({
    mutationFn: (vars: { email: string; password: string; confirmPassword: string }) =>
      signUp(vars.email, vars.password, vars.confirmPassword),
  });
}

export function useForgotPassword() {
  return useMutation({
    mutationFn: (vars: { email: string }) =>
      authApi.forgotPassword({ email: vars.email.trim().toLowerCase() }),
  });
}

export function useResetPassword() {
  return useMutation({
    mutationFn: (vars: { token: string; newPassword: string }) =>
      authApi.resetPassword({ token: vars.token, newPassword: vars.newPassword }),
  });
}

export function useVerifyEmail() {
  return useMutation({
    mutationFn: (token: string) => authApi.verifyEmail({ token }),
  });
}
