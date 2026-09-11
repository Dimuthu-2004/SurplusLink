# ADR 0001: Client and service boundaries

- **Status:** Accepted
- **Decision:** ASP.NET Core Web API is the only public backend. React and Flutter communicate only with it. The API uses EF Core with PostgreSQL and is the sole caller of the FastAPI/LangGraph service.
- **Consequences:** No PostgreSQL credentials, PostgreSQL drivers, AI service URL, or AI credentials are placed in web/mobile applications. The AI service is deployed on a private network and accepts traffic only from the API.
