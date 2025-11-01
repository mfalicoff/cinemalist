import type { PageServerLoad } from "./$types";
import { fetchScraperHistory } from "$lib/api/scraper";

export const load: PageServerLoad = async () => {
  try {
    const history = await fetchScraperHistory();
    return {
      history,
    };
  } catch (error) {
    console.error("Error loading scraper history:", error);
    return {
      history: [],
      error: "Failed to load scraper history from the backend",
    };
  }
};
