import { useState, useEffect, useRef } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import ReactMarkdown from "react-markdown";
import { apiClient } from "../../api/client";
import { Button } from "../common/Button";
import { LoadingSpinner } from "../common/Loading";
import { EmptyState } from "../common/EmptyState";
import { useToast } from "../hooks/use-toast";
import type { RagResponse, RagSource, IndexedDocument } from "../../types";
import { generateId, formatDate } from "../../utils";
import {
  Send,
  MessageSquare,
  PlusCircle,
  Copy,
  Bot,
  User,
  X,
  FileText,
} from "lucide-react";

interface ChatMessage {
  id: string;
  role: "user" | "assistant";
  content: string;
  sources?: RagSource[];
  conversationId: string;
  timestamp: string;
}

export function ChatPage() {
  const { toast } = useToast();
  const [conversationId, setConversationId] = useState<string>(generateId());
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [question, setQuestion] = useState("");
  const [isChatting, setIsChatting] = useState(false);
  const [documents, setDocuments] = useState<IndexedDocument[]>([]);
  const [showDocuments, setShowDocuments] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const { data: docs } = useQuery({
    queryKey: ["documents"],
    queryFn: () => apiClient.getDocuments(),
  });

  useEffect(() => {
    if (docs) {
      setDocuments(docs);
    }
  }, [docs]);

  const askMutation = useMutation({
    mutationFn: ({ conversationId, question }: { conversationId: string; question: string }) =>
      apiClient.askQuestion(conversationId, question),
    onSuccess: (data: RagResponse) => {
      const assistantMessage: ChatMessage = {
        id: generateId(),
        role: "assistant",
        content: data.answer,
        sources: data.sources,
        conversationId,
        timestamp: new Date().toISOString(),
      };
      setMessages((prev) => [...prev, assistantMessage]);
      setIsChatting(false);
      setQuestion("");
      if (textareaRef.current) {
        textareaRef.current.style.height = "auto";
      }
    },
    onError: (error: unknown) => {
      setIsChatting(false);
      if (textareaRef.current) {
        textareaRef.current.style.height = "auto";
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to get response from AI";
      toast({
        title: "Error",
        description: errorMessage,
        variant: "destructive",
      });
    },
  });


  const handleNewChat = () => {
    setConversationId(generateId());
    setMessages([]);
  };

  const handleSendMessage = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!question.trim() || isChatting) return;

    const userMessage: ChatMessage = {
      id: generateId(),
      role: "user",
      content: question,
      conversationId,
      timestamp: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setIsChatting(true);

    await askMutation.mutateAsync({ conversationId, question });
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toast({
      title: "Copied",
      description: "Answer copied to clipboard",
    });
  };

  const formatSource = (source: RagSource): string => {
    const fileName = source.source.split("/").pop() || source.source;
    return `-${fileName} (Chunk ${source.chunkIndex}, Score: ${source.score.toFixed(2)})`;
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, isChatting]);

  return (
    <div className="flex h-[calc(100vh-140px)] flex-col gap-4 lg:flex-row">
      {/* Chat Area */}
      <div className="flex-1 flex flex-col rounded-lg border bg-background">
        {/* Chat Header */}
        <div className="flex items-center justify-between border-b px-4 py-3">
          <div className="flex items-center gap-2">
            <MessageSquare className="h-5 w-5 text-primary" />
            <h2 className="font-semibold">AI Assistant</h2>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowDocuments(!showDocuments)}
            >
              <FileText className="mr-2 h-4 w-4" />
              {showDocuments ? "Hide" : "Show"} Docs
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={handleNewChat}
              title="Start new conversation"
            >
              <PlusCircle className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {/* Messages */}
        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          {messages.length === 0 ? (
            <EmptyState
              title="Start a conversation"
              description={
                isChatting
                  ? "AI is thinking..."
                  : "Ask a question about your documents to get started"
              }
              icon={
                isChatting ? (
                  <LoadingSpinner />
                ) : (
                  <Bot className="h-12 w-12 text-primary" />
                )
              }
            />
          ) : (
            messages.map((msg) => (
              <div
                key={msg.id}
                className={`flex gap-3 ${
                  msg.role === "user" ? "flex-row-reverse" : ""
                }`}
              >
                <div
                  className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full ${
                    msg.role === "user"
                      ? "bg-primary text-primary-foreground"
                      : "bg-secondary text-secondary-foreground"
                  }`}
                >
                  {msg.role === "user" ? <User className="h-5 w-5" /> : <Bot className="h-5 w-5" />}
                </div>
                <div
                  className={`max-w-[85%] rounded-lg px-4 py-3 ${
                    msg.role === "user"
                      ? "bg-primary text-primary-foreground"
                      : "bg-muted text-foreground"
                  }`}
                >
                  <div className="prose prose-sm max-w-none">
                    <ReactMarkdown>{msg.content}</ReactMarkdown>
                  </div>
                  {msg.sources && msg.sources.length > 0 && (
                    <div className="mt-3 pt-3 border-t border-current/20">
                      <p className="text-xs font-medium mb-2">Sources:</p>
                      <ul className="space-y-1">
                        {msg.sources.map((source, idx) => (
                          <li key={idx} className="text-xs text-muted-foreground">
                            {formatSource(source)}
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                  <div className="mt-2 flex gap-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => copyToClipboard(msg.content)}
                      className="h-6 px-2 text-xs"
                    >
                      <Copy className="mr-1 h-3 w-3" />
                      Copy
                    </Button>
                  </div>
                </div>
              </div>
            ))
          )}
          <div ref={messagesEndRef} />
        </div>

        {/* Input Area */}
        <div className="border-t p-4">
          <form onSubmit={handleSendMessage} className="relative flex gap-2">
            <div className="relative flex-1">
              <textarea
                ref={textareaRef}
                value={question}
                onChange={(e) => {
                  setQuestion(e.target.value);
                  e.target.style.height = "auto";
                  e.target.style.height = `${Math.min(e.target.scrollHeight, 200)}px`;
                }}
                onKeyDown={handleKeyDown}
                placeholder={
                  isChatting ? "AI is thinking..." : "Ask a question about your documents..."
                }
                className="w-full resize-none rounded-md border border-input bg-background px-4 py-3 pr-12 focus:outline-none focus:ring-2 focus:ring-ring"
                rows={1}
                disabled={isChatting}
              />
            </div>
            <Button
              type="submit"
              disabled={!question.trim() || isChatting}
              className="h-10 w-10 shrink-0 rounded-full"
            >
              {isChatting ? (
                <LoadingSpinner />
              ) : (
                <Send className="h-5 w-5" />
              )}
            </Button>
          </form>
          <p className="mt-2 text-center text-xs text-muted-foreground">
            Press Enter to send, Shift+Enter for new line
          </p>
        </div>
      </div>

      {/* Documents Panel */}
      {showDocuments && (
        <div className="w-full overflow-y-auto rounded-lg border bg-background lg:w-80">
          <div className="flex items-center justify-between border-b px-4 py-3">
            <h3 className="font-semibold">Indexed Documents</h3>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setShowDocuments(false)}
              className="lg:hidden"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
          {documents.length === 0 ? (
            <div className="p-4 text-center text-sm text-muted-foreground">
              No documents indexed yet
            </div>
          ) : (
            <div className="max-h-[60vh] overflow-y-auto">
              {documents.map((doc) => (
                <div key={doc.documentId} className="border-b p-4 last:border-0">
                  <div className="flex items-start gap-3">
                    <FileText className="h-6 w-6 shrink-0 text-primary" />
                    <div className="flex-1">
                      <h4 className="font-medium">{doc.fileName}</h4>
                      <div className="mt-1 flex flex-wrap gap-2 text-xs text-muted-foreground">
                        <span>{doc.chunkCount} chunks</span>
                        {doc.fileSizeBytes && (
                          <span>• {Math.round(doc.fileSizeBytes / 1024)} KB</span>
                        )}
                        {doc.updatedAt && <span>• {formatDate(doc.updatedAt)}</span>}
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}