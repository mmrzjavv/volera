export interface Call {
  id: string;
  callerId: string;
  callerName?: string;
  receiverId: string;
  receiverName?: string;
  startTime: string;
  endTime?: string;
  duration?: number;
  status: 'Ringing' | 'Connected' | 'Ended' | 'Missed';
  isVideo?: boolean;
}

export interface InitiateCallResponse {
  callId: string;
}

export interface IceServerConfig {
  urls: string | string[];
  username?: string;
  credential?: string;
}

export interface IceServersResponse {
  iceServers: IceServerConfig[];
}

export interface WebRTCSignal {
  type: 'offer' | 'answer' | 'ice-candidate';
  data: any;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CallHistoryParams {
  page?: number;
  pageSize?: number;
  term?: string;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: string;
  sortDescending?: boolean;
}
