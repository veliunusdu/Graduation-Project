from fastapi import FastAPI
from config.settings import settings

app = FastAPI(title=settings.APP_NAME)

@app.get("/")
def read_root():
    return {"status": "ok", "message": "Python AI Service is running"}

@app.get("/health")
def health_check():
    """
    Python servisinin sağlık kontrolünü yapar ve başarılı olduğunda 
    beklenen JSON cevabını döndürür.
    """
    return {"status": "ok", "ollama_url": settings.OLLAMA_BASE_URL}
