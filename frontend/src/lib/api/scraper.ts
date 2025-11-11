import { env } from "$env/dynamic/public";
import type { ScraperHistoryEntity } from "$lib/types/scraper";

export async function fetchScraperHistory(): Promise<ScraperHistoryEntity[]> {
  try {
    const response = await fetch(`${env.PUBLIC_API_URL}/api/scraper/history`);

    if (!response.ok) {
      throw new Error(
        `Failed to fetch scraper history: ${response.statusText}`,
      );
    }

    const history = (await response.json()).result;
    return history;
  } catch (error) {
    console.error("Error fetching scraper history:", error);
    throw error;
  }
}
