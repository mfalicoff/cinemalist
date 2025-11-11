import type { PageServerLoad, Actions } from "./$types";
import { fetchAllFilms, addToRadarr } from "$lib/api/films";
import { fail } from "@sveltejs/kit";

export const load: PageServerLoad = async () => {
  try {
    const films = await fetchAllFilms();
    return {
      films,
    };
  } catch (error) {
    console.error("Error loading films:", error);
    return {
      films: [],
      error: "Failed to load films from the backend",
    };
  }
};

export const actions: Actions = {
  addToRadarr: async ({ request }) => {
    const data = await request.formData();
    const tmdbId = data.get("tmdbId");

    if (!tmdbId || typeof tmdbId !== "string") {
      return fail(400, { error: "Invalid TMDB ID" });
    }

    try {
      await addToRadarr(tmdbId);
      return { success: true, tmdbId };
    } catch (error) {
      console.error("Error adding film to Radarr:", error);
      return fail(500, { error: "Failed to add film to Radarr" });
    }
  },
};
