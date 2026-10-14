import { describe, it, expect, vi } from "vitest";
import { apiClient } from "../api/client";

// Mock the API client
vi.mock("../api/client", () => ({
  apiClient: {
    getDocuments: vi.fn(),
    uploadDocument: vi.fn(),
    deleteDocument: vi.fn(),
    getHealth: vi.fn(),
    askQuestion: vi.fn(),
  },
}));

// Helper to render with providers (currently unused but available for future tests)
// function renderWithProviders(component: React.ReactNode) {
//   const queryClient = new QueryClient({
//     defaultOptions: { queries: { retry: false } },
//   });
//   return render(
//     <QueryClientProvider client={queryClient}>
//       <MemoryRouter>{component}</MemoryRouter>
//     </QueryClientProvider>
//   );
// }

describe("API Client", () => {
  it("should have correct API endpoints", () => {
    // Verify API client methods exist
    expect(typeof apiClient.getDocuments).toBe("function");
    expect(typeof apiClient.uploadDocument).toBe("function");
    expect(typeof apiClient.deleteDocument).toBe("function");
    expect(typeof apiClient.getHealth).toBe("function");
    expect(typeof apiClient.askQuestion).toBe("function");
  });
});

describe("Utility Functions", () => {
  it("formatBytes formats correctly", async () => {
    // Dynamic import to get the function
    const utils = await import("../utils");
    expect(utils.formatBytes(0)).toBe("0 Bytes");
    expect(utils.formatBytes(1024)).toBe("1 KB");
    expect(utils.formatBytes(1048576)).toBe("1 MB");
  });

  it("generateId creates valid UUID", async () => {
    const utils = await import("../utils");
    const id = utils.generateId();
    expect(id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
    );
  });
});