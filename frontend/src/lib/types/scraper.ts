export interface ScraperHistoryEntity {
  id?: string;
  scrapeDate: string;
  source: string;
  moviesScraped: Record<string, string>;
}
