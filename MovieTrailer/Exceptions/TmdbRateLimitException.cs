namespace MovieTrailer.Exceptions;

public class TmdbRateLimitException()
    : Exception("TMDB rate limit reached. Try again shortly.");
