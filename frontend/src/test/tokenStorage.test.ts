import { describe, expect, it } from "vitest";
import { clearAccessToken, getAccessToken, saveAccessToken } from "../auth/tokenStorage";

describe("tokenStorage", () => {
  it("saves, reads and clears access token", () => {
    clearAccessToken();
    expect(getAccessToken()).toBeNull();

    saveAccessToken("t1");
    expect(getAccessToken()).toBe("t1");

    clearAccessToken();
    expect(getAccessToken()).toBeNull();
  });
});

