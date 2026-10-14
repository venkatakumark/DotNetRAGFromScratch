import axios, { type AxiosInstance, type AxiosError, type InternalAxiosRequestConfig } from "axios";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5149";

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        "Content-Type": "application/json",
      },
    });

    this.client.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        if (config.data instanceof FormData) {
          delete config.headers["Content-Type"];
        }
        return config;
      },
      (error: unknown) => Promise.reject(error)
    );

    this.client.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        console.error("API Error:", error.response?.data || error.message);
        return Promise.reject(error);
      }
    );
  }

  // Health
  async getHealth() {
    const response = await this.client.get("/health");
    return response.data;
  }

  // Documents
  async getDocuments() {
    const response = await this.client.get("/api/documents");
    return response.data;
  }

  async uploadDocument(file: File) {
    const formData = new FormData();
    formData.append("file", file);
    const response = await this.client.post("/api/documents/upload", formData);
    return response.data;
  }

  async replaceDocument(documentId: string, file: File) {
    const formData = new FormData();
    formData.append("file", file);
    const response = await this.client.put(`/api/documents/${documentId}`, formData);
    return response.data;
  }

  async deleteDocument(documentId: string) {
    const response = await this.client.delete(`/api/documents/${documentId}`);
    return response.data;
  }

  // RAG Chat
  async askQuestion(conversationId: string, question: string) {
    const response = await this.client.post("/api/rag/ask", {
      conversationId,
      question,
    });
    return response.data;
  }

  // Conversations
  async deleteConversation(conversationId: string) {
    const response = await this.client.delete(`/api/conversations/${conversationId}`);
    return response.data;
  }
}

export const apiClient = new ApiClient();