/** Standard API response envelope from the backend */
export interface ApiResponse<T = unknown> {
  success: boolean;
  operationDate: string;
  data: T | null;
  message?: string | string[] | null;
}

export interface RecentChat {
    userId?: string;
    groupId?: string;
    name?: string;
    firstName?: string;
    lastName?: string;
    username?: string;
    profilePicture?: string;
    lastMessageContent: string;
    lastMessageAt: string;
    unreadCount: number;
    isOnline: boolean;
    isGroup?: boolean;
    isChannel?: boolean;
    publicUsername?: string;
}

export interface User {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email?: string;
  bio?: string;
  phoneNumber?: string;
  profilePicture?: string;
  isOnline: boolean;
  lastSeen?: Date;
  role?: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  email?: string;
  bio?: string;
  profilePicture?: string;
}

export interface SessionInfo {
  id: string;
  userId: string;
  deviceType: string;
  browser: string;
  os: string;
  location: string;
  loginAt: string;
  lastActivityAt: string;
  appVersion: string;
  isRevoked: boolean;
}

export interface Contact {
  id: string;
  ownerUserId: string;
  contactUserId?: string;
  contactUser?: User;
  contactName: string;
  contactPhoneNumber: string;
  status: string; // "Added", "Pending", "Blocked"
  createdAt: string;
  updatedAt: string;
}

export interface AddContactDto {
  contactIdentifier: string; // Username, phone number, or User ID
  contactName: string;
}

export interface SyncContactsDto {
  phoneNumbers: string[];
}

export interface Message {
  id: string;
  senderId: string;
  receiverId?: string;
  groupId?: string;
  content: string;
  attachmentUrl?: string;
  attachmentType?: string;
  sentAt: string;
  isRead: boolean;
  isEdited?: boolean;
  isSaved?: boolean;
  deletedAt?: string | null;
  replyToMessageId?: string;
  replyToMessagePreview?: {
    id: string;
    senderId: string;
    senderName: string;
    contentSnippet: string;
    deletedAt?: string | null;
  } | null;
  replyToStoryItemId?: string;
  replyToStoryItemPreview?: {
    storyItemId: string;
    storyOwnerId: string;
    ownerName: string;
    mediaUrl?: string;
    mediaType: string;
    overlaySnippet?: string;
  } | null;
  reactions?: {
    userId: string;
    userName?: string;
    emoji: string;
  }[];
  forwardedFromMessageId?: string;
  forwardedAt?: string;
  isPinned?: boolean;
  pinnedAt?: string;
  pinnedByUserId?: string;
  /** Client idempotency key for offline-first sends */
  clientMessageId?: string;
  /** Local delivery UI state for outbound messages */
  deliveryStatus?: 'queued' | 'sending' | 'accepted' | 'retrying' | 'permanently_failed' | 'cancelled';
  signatureDisplayName?: string;
  viewCount?: number;
  sendAsChannelId?: string;
  sendAsChannelName?: string;
  sendAsChannelProfilePictureUrl?: string;
}

export interface Group {
    id: string;
    name: string;
    adminId: string;
    createdAt: string;
    profilePictureUrl?: string | null;
    isChannel?: boolean;
    kind?: string;
    canPost?: boolean;
    isPublic?: boolean;
    publicUsername?: string | null;
    signaturesEnabled?: boolean;
    linkedDiscussionGroupId?: string | null;
    subscriberCount?: number;
    isAdmin?: boolean;
}

export interface ChannelDetails extends Group {
    description?: string | null;
    inviteCode?: string | null;
    canManageSubscribers?: boolean;
    canChangeInfo?: boolean;
    canAddAdmins?: boolean;
    canEditMessages?: boolean;
    canDeleteMessages?: boolean;
    members?: User[];
    admins?: ChannelMember[];
}

export interface ChannelMember {
    userId: string;
    username: string;
    firstName: string;
    lastName: string;
    profilePicture?: string;
    isAdmin: boolean;
    canPost: boolean;
    canEditMessages: boolean;
    canDeleteMessages: boolean;
    canManageSubscribers: boolean;
    canChangeInfo: boolean;
    canAddAdmins: boolean;
    joinedAt: string;
}

export interface ChannelAnalytics {
    channelId: string;
    subscriberCount: number;
    postCount: number;
    totalViews: number;
    postsLast7Days: number;
}

export interface SuggestedPost {
    id: string;
    channelId: string;
    fromUserId: string;
    fromUserName: string;
    content: string;
    attachmentUrl?: string;
    attachmentType?: string;
    status: string;
    scheduledAt?: string;
    adminNote?: string;
    publishedMessageId?: string;
    createdAt: string;
}

export interface CreateGroupRequest {
    name: string;
    memberIds: string[];
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  username: string;
  phoneNumber: string;
  password: string;
}

export interface SystemMessage {
  id: string;
  title: string;
  content: string;
  createdAt: string;
  expiresAt?: string;
  isActive: boolean;
  isRead: boolean;
}

export interface Plan {
  id: string;
  name: string;
  description?: string;
  price: number;
  maxBranches: number;
  maxSupportUsers: number;
  maxMessagesPerMonth: number;
}

export interface StoryTextOverlay {
  text: string;
  color?: string;
  x?: number;
  y?: number;
  fontScale?: number;
}

export interface StoryItem {
  id: string;
  mediaType: 'Image' | 'Video' | string;
  objectKey: string;
  mediaUrl?: string;
  durationMs: number;
  textOverlayJson?: string | null;
  sortOrder: number;
}

export interface Story {
  storyId: string;
  createdAt: string;
  expiresAt: string;
  viewedByMe: boolean;
  items: StoryItem[];
}

export interface StoryRing {
  userId: string;
  displayName: string;
  profilePicture?: string;
  hasUnseen: boolean;
  isOwn: boolean;
  latestCreatedAt: string;
  stories: Story[];
}

export interface StoryViewer {
  userId: string;
  displayName: string;
  profilePicture?: string;
  viewedAt: string;
}

export interface CreateStoryItemPayload {
  objectKey: string;
  mediaType: 'Image' | 'Video';
  durationMs?: number;
  textOverlayJson?: string;
}
