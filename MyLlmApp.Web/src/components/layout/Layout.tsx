import { Outlet } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { apiClient } from "../../api/client";
import { Sidebar } from "./Sidebar";

export function Layout() {
  const { data: health } = useQuery({
    queryKey: ["health"],
    queryFn: () => apiClient.getHealth(),
    refetchInterval: 30000,
    retry: 1,
  });

  // Determine health status from the response
  let healthStatus: "healthy" | "degraded" | "unhealthy" | null = null;
    if (health === "Healthy" || health === "healthy" || health?.status === "healthy") {
      healthStatus = "healthy";
    } else if (health === "Degraded" || health === "degraded" || health?.status === "degraded") {
      healthStatus = "degraded";
    } else if (health && health.status !== undefined) {
      healthStatus = "unhealthy";
    }

  return (
    <div className="flex min-h-screen">
      <Sidebar healthStatus={healthStatus} />
      <main className="flex-1 lg:pl-64">
        <div className="container mx-auto p-4 lg:p-8">
          <Outlet />
        </div>
      </main>
    </div>
  );
}