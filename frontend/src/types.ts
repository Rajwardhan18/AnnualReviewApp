export type UserType = 'Developer' | 'Manager' | 'Admin'
export type GoalType = 'Professional' | 'Personal'
export type GoalStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'Dropped'
export type ReviewStatus = 'Draft' | 'Submitted' | 'InReview' | 'Completed'
export type ReviewerType = 'Manager' | 'Peer'

export interface User {
  id: number
  fullName: string
  email: string
  userType: UserType
  functionId?: number | null
  functionName?: string | null
  roleId?: number | null
  roleName?: string | null
  isActive: boolean
}

export interface AuthResponse { token: string; user: User }

export interface FunctionItem { id: number; name: string; description?: string | null }
export interface Role { id: number; name: string; functionId: number; functionName: string }
export interface Skill { id: number; name: string; category?: string | null }
export interface CompanyTrait { id: number; name: string; description?: string | null }

export interface Cycle {
  id: number; name: string; year: number
  startDate: string; endDate: string
  isReleased: boolean; isActive: boolean; reviewCount: number
  dueDate?: string | null
  halfYearlyReleased: boolean
  halfYearlyDueDate?: string | null
  ratingsReleased: boolean
  ended: boolean
}

export type NotificationType = 'PlanReleased' | 'HalfYearlyReleased' | 'ReviewerAssigned' | 'Reminder'
export interface AppNotification {
  id: number
  type: NotificationType
  subject: string
  body: string
  reviewCycleId?: number | null
  reviewId?: number | null
  isRead: boolean
  emailSent: boolean
  createdAt: string
}

export interface Goal {
  id: number
  goalType: GoalType
  title: string
  specific: string
  measurable: string
  achievable: string
  relevant: string
  timeBound: string
  companyTraitId: number | null
  companyTraitName?: string | null
  status: GoalStatus
  completionPercentage: number
  statusComment?: string | null
  statusDate?: string | null
  target?: string | null
}

export interface GoalInput {
  goalType: GoalType
  title: string
  specific: string
  measurable: string
  achievable: string
  relevant: string
  timeBound: string
  companyTraitId: number | null
  status: GoalStatus
  completionPercentage: number
  statusComment?: string | null
  statusDate?: string | null
  target?: string | null
}

export interface GoalProgressInput {
  goalId: number
  status: GoalStatus
  completionPercentage: number
  statusComment?: string | null
  statusDate?: string | null
}

export interface Achievement {
  id: number
  projectName: string
  clientName: string
  workDescription: string
  manager1Rating?: number | null
  manager2Rating?: number | null
  companyTraitId?: number | null
  companyTraitName?: string | null
}

export interface AchievementInput {
  projectName: string
  clientName: string
  workDescription: string
  companyTraitId?: number | null
}

export interface RndImprovement { id: number; description: string }
export interface FutureSkill { id: number; name: string }

export interface SkillRating { skillId: number; skillName: string; selfRating: number; comments?: string | null }
export interface SkillRatingInput { skillId: number; selfRating: number; comments?: string | null }

export interface Reviewer { reviewerId: number; reviewerName: string; reviewerType: ReviewerType; hasSubmitted: boolean }

export interface ReviewerSkillRating { skillId: number; skillName: string; rating: number }
export interface Assessment {
  id: number
  reviewerId: number
  reviewerName: string
  reviewerType: ReviewerType
  overallRating: number
  strengths?: string | null
  improvements?: string | null
  submittedAt?: string | null
  skillRatings: ReviewerSkillRating[]
}

export interface ReviewSummary {
  id: number
  cycleId: number
  cycleName: string
  developerId: number
  developerName: string
  functionName?: string | null
  roleName?: string | null
  status: ReviewStatus
  submittedAt?: string | null
  halfYearlyReleased: boolean
  midYearSubmitted: boolean
  myAssessmentSubmitted?: boolean | null
}

export interface ReviewDetail {
  id: number
  cycleId: number
  cycleName: string
  developerId: number
  developerName: string
  functionId?: number | null
  functionName?: string | null
  roleId?: number | null
  roleName?: string | null
  status: ReviewStatus
  submittedAt?: string | null
  selectedPeerId?: number | null
  selectedPeerName?: string | null
  selfSummary?: string | null
  midYearReflection?: string | null
  midYearSubmittedAt?: string | null
  halfYearlyReleased: boolean
  halfYearlyDueDate?: string | null
  dueDate?: string | null
  goals: Goal[]
  achievements: Achievement[]
  rndImprovements: RndImprovement[]
  futureSkills: FutureSkill[]
  skillRatings: SkillRating[]
  roleSkills: Skill[]
  reviewers: Reviewer[]
  assessments: Assessment[]
  myManagerSlot?: number | null
  /** True once this manager may no longer change their achievement ratings. */
  achievementRatingsLocked?: boolean
}

// ---- Ratings dashboard ----
export interface RatingWeights { self: number; peer: number; manager1: number; manager2: number }
export interface BandBucket { band: string; count: number; lowerZ: number | null; upperZ: number | null }
export interface CurveStats { count: number; mean: number; stdDev: number; min: number; max: number; buckets: BandBucket[] }

export interface DeveloperRatingRow {
  reviewId: number
  developerId: number
  developerName: string
  functionName?: string | null
  roleName?: string | null
  cycleId: number
  cycleName: string
  status: ReviewStatus
  selfScore: number | null
  peerScore: number | null
  manager1Score: number | null
  manager2Score: number | null
  weightedFinal: number | null
  zScore: number | null
  percentile: number | null
  curvedScore: number | null
  band: string | null
}

export interface RatingsDashboard {
  cycleId: number
  cycleName: string
  weights: RatingWeights
  curve: CurveStats
  developers: DeveloperRatingRow[]
}

export interface MyGoalProgress {
  id: number
  title: string
  goalType: GoalType
  status: GoalStatus
  completionPercentage: number
  target?: string | null
}

export interface MyPerformanceCycle {
  reviewId: number
  cycleId: number
  cycleName: string
  status: ReviewStatus
  ratingsReleased: boolean
  selfScore: number | null
  peerScore: number | null
  manager1Score: number | null
  manager2Score: number | null
  weightedFinal: number | null
  overallAverage: number | null
  percentile: number | null
  band: string | null
  teamAverage: number | null
  goalCount: number
  avgCompletion: number
  completed: number
  inProgress: number
  notStarted: number
  dropped: number
  goals: MyGoalProgress[]
}

export interface MyPerformance { cycles: MyPerformanceCycle[] }

export interface SavePlanRequest {
  selectedPeerId?: number | null
  selfSummary?: string | null
  goals: GoalInput[]
  skillRatings: SkillRatingInput[]
  achievements: AchievementInput[]
  rndImprovements: { description: string }[]
  futureSkills: { name: string }[]
}
