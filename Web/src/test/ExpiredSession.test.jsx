import { AxiosError } from "axios";
import { afterEach, expect, it } from "vitest";
import api from "../services/api";
import { getAuthToken, setAuthToken } from "../services/authSession";

afterEach(() => {
  sessionStorage.clear();
  localStorage.clear();
});

it("clears an expired bearer session after Auth/me returns 401", async () => {
  setAuthToken("test-session");
  await expect(api.get("/Auth/me", {
    adapter: async (config) => {
      throw new AxiosError("Unauthorized", "ERR_BAD_REQUEST", config, null,
        { status: 401, statusText: "Unauthorized", data: {}, headers: {}, config });
    },
  })).rejects.toMatchObject({ response: { status: 401 } });

  expect(getAuthToken()).toBe("");
  expect(sessionStorage.getItem("gemora_session_expired")).toBe("1");
});
