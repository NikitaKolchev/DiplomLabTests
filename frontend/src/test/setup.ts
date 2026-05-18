import '@testing-library/jest-dom';
import { vi } from "vitest";

Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn()
  })
});

const originalGetComputedStyle = window.getComputedStyle.bind(window);
Object.defineProperty(window, "getComputedStyle", {
  writable: true,
  value: (elt: Element, pseudoElt?: string | null) => originalGetComputedStyle(elt)
});

const localStorageStore = new Map<string, string>();

Object.defineProperty(window, "localStorage", {
  configurable: true,
  value: {
    getItem: (key: string) => (localStorageStore.has(key) ? localStorageStore.get(key)! : null),
    setItem: (key: string, value: string) => {
      localStorageStore.set(key, String(value));
    },
    removeItem: (key: string) => {
      localStorageStore.delete(key);
    },
    clear: () => {
      localStorageStore.clear();
    }
  }
});

Object.defineProperty(window, "scrollTo", {
  writable: true,
  value: vi.fn()
});

class ResizeObserverMock {
  observe() {}
  unobserve() {}
  disconnect() {}
}

Object.defineProperty(window, "ResizeObserver", {
  writable: true,
  value: ResizeObserverMock
});
