namespace Commerce.Domain;

public enum TicketCategory { Refund, Shipping, Warranty, Technical, Payment, Complaint, Other }

public enum TicketUrgency { Low, Medium, High }

public enum TicketSentiment { Positive, Neutral, Negative }

public enum TicketStatus { Queued, Triaged, Escalated, Resolved, Discarded }

public enum KnowledgeSourceType { Product, Manual, Faq, Policy, Review, KbArticle }

public enum NotificationSeverity { Info, Warning, Critical }

public enum OrderStatus { Pending, Completed, Cancelled }
