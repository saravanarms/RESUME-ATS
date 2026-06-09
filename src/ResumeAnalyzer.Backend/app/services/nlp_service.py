import re
from typing import Dict, List, Any

# Pure-python fallback stop words (standard English stop words)
DEFAULT_STOP_WORDS = {
    'i', 'me', 'my', 'myself', 'we', 'our', 'ours', 'ourselves', 'you', "you're", "you've", "you'll", "you'd",
    'your', 'yours', 'yourself', 'yourselves', 'he', 'him', 'his', 'himself', 'she', "she's", 'her', 'hers',
    'herself', 'it', "it's", 'its', 'itself', 'they', 'them', 'their', 'theirs', 'themselves', 'what', 'which',
    'who', 'whom', 'this', 'that', "that'll", 'these', 'those', 'am', 'is', 'are', 'was', 'were', 'be', 'been',
    'being', 'have', 'has', 'had', 'having', 'do', 'does', 'did', 'doing', 'a', 'an', 'the', 'and', 'but', 'if',
    'or', 'because', 'as', 'until', 'while', 'of', 'at', 'by', 'for', 'with', 'about', 'against', 'between',
    'into', 'through', 'during', 'before', 'after', 'above', 'below', 'to', 'from', 'up', 'down', 'in', 'out',
    'on', 'off', 'over', 'under', 'again', 'further', 'then', 'once', 'here', 'there', 'when', 'where', 'why',
    'how', 'all', 'any', 'both', 'each', 'few', 'more', 'most', 'other', 'some', 'such', 'no', 'nor', 'not',
    'only', 'own', 'same', 'so', 'than', 'too', 'very', 's', 't', 'can', 'will', 'just', 'don', "don't", 'should',
    "should've", 'now', 'd', 'll', 'm', 'o', 're', 've', 'y', 'ain', 'aren', "aren't", 'couldn', "couldn't",
    'didn', "didn't", 'doesn', "doesn't", 'hadn', "hadn't", 'hasn', "hasn't", 'haven', "haven't", 'isn', "isn't",
    'ma', 'mightn', "mightn't", 'mustn', "mustn't", 'needn', "needn't", 'shan', "shan't", 'shouldn', "shouldn't",
    'wasn', "wasn't", 'weren', "weren't", 'won', "won't", 'wouldn', "wouldn't"
}

class NLPService:
    def __init__(self):
        # Attempt to load spaCy and NLTK if available; otherwise run in pure Python mode
        self.use_spacy = False
        self.stop_words = DEFAULT_STOP_WORDS

        try:
            import spacy
            try:
                self.nlp = spacy.load("en_core_web_sm")
                self.use_spacy = True
            except OSError:
                # Attempt to download model
                try:
                    spacy.cli.download("en_core_web_sm")
                    self.nlp = spacy.load("en_core_web_sm")
                    self.use_spacy = True
                except Exception:
                    self.nlp = None
                    self.use_spacy = False
        except ImportError:
            self.nlp = None

        try:
            import nltk
            from nltk.corpus import stopwords
            try:
                self.stop_words = set(stopwords.words('english'))
            except Exception:
                try:
                    nltk.download('stopwords')
                    nltk.download('punkt')
                    self.stop_words = set(stopwords.words('english'))
                except Exception:
                    self.stop_words = DEFAULT_STOP_WORDS
        except ImportError:
            pass

        # Standard technical and soft skills dataset
        self.SKILLS_DB = [
            # Languages
            "python", "javascript", "typescript", "c#", "java", "c++", "c", "ruby", "go", "golang", "rust", "php", "swift", "kotlin", "sql", "html", "css", "sass", "r", "scala", "shell", "bash",
            # Frameworks / Libraries
            "fastapi", "django", "flask", "asp.net", "net core", "spring boot", "react", "angular", "vue", "next.js", "nuxt", "svelte", "express", "node.js", "jquery", "laravel", "rails", "pytorch", "tensorflow", "keras", "pandas", "numpy", "scikit-learn", "spacy", "nltk", "opencv", "hibernate", "entity framework", "ef core",
            # Cloud / DevOps
            "aws", "azure", "gcp", "google cloud", "docker", "kubernetes", "k8s", "jenkins", "terraform", "ansible", "ci/cd", "github actions", "gitlab ci", "argocd", "prometheus", "grafana", "nginx", "apache", "linux", "unix",
            # Databases
            "postgresql", "mysql", "mongodb", "redis", "sqlite", "sql server", "mssql", "oracle", "mariadb", "cassandra", "elasticsearch", "dynamodb", "firebase",
            # Tools / Concepts
            "git", "github", "gitlab", "jira", "confluence", "trello", "scrum", "agile", "kanban", "rest api", "graphql", "grpc", "soap", "microservices", "serverless", "oop", "solid", "dry", "mvc", "mvvm", "clean architecture",
            # Soft Skills / Business
            "communication", "leadership", "teamwork", "problem solving", "time management", "critical thinking", "adaptability", "project management", "active listening", "negotiation", "collaboration", "analytical skills"
        ]

        # Standard Certifications list
        self.CERTIFICATIONS_DB = [
            "aws certified", "azure administrator", "azure solutions architect", "pmp", "project management professional", "scrum master", "csm", "comptia", "cissp", "ccna", "ccnp", "google cloud professional", "itil", "six sigma"
        ]

    def extract_emails(self, text: str) -> List[str]:
        email_pattern = r'[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+'
        return list(set(re.findall(email_pattern, text)))

    def extract_phones(self, text: str) -> List[str]:
        phone_pattern = r'(?:(?:\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4})'
        return list(set(re.findall(phone_pattern, text)))

    def extract_skills(self, text: str) -> List[str]:
        text_lower = text.lower()
        found_skills = []
        
        if self.use_spacy and self.nlp:
            # Tokenize using spaCy
            doc = self.nlp(text_lower)
            tokens = [token.text for token in doc if not token.is_stop and not token.is_punct]
        else:
            # Tokenize using regex (pure python fallback)
            tokens = re.findall(r'\b[a-zA-Z0-9+#.-]+\b', text_lower)
            tokens = [t for t in tokens if t not in self.stop_words]
        
        # Find matches
        for skill in self.SKILLS_DB:
            if " " in skill: # Multi-word skill
                if skill in text_lower:
                    found_skills.append(skill)
            else:
                if skill in tokens:
                    found_skills.append(skill)
                    
        return list(set(found_skills))

    def extract_education(self, text: str) -> List[Dict[str, Any]]:
        text_lower = text.lower()
        education = []
        
        degree_patterns = {
            "Doctorate (PhD)": [r"ph\.?d", "doctor of philosophy", "doctorate"],
            "Master's Degree": [r"m\.?s\.?c?", r"m\.?tech", "master of science", "master of computer applications", "mca", "master's", "mba", "master of business administration"],
            "Bachelor's Degree": [r"b\.?s\.?c?", r"b\.?tech", r"b\.?e\.?", "bachelor of science", "bachelor of engineering", "bachelor's", "bca", "bachelor of computer applications"],
            "Associate's Degree": ["associate degree", "associate of science", "associate of arts"]
        }
        
        lines = text.split('\n')
        for line in lines:
            line_lower = line.lower()
            for degree, patterns in degree_patterns.items():
                matched = False
                for pattern in patterns:
                    if re.search(pattern, line_lower):
                        major = "Not specified"
                        major_match = re.search(r'(?:in|of|major in)\s+([a-zA-Z\s,]{3,30})', line, re.IGNORECASE)
                        if major_match:
                            major = major_match.group(1).strip()
                            
                        education.append({
                            "degree": degree,
                            "raw_text": line.strip(),
                            "major": major
                        })
                        matched = True
                        break
                if matched:
                    break
        
        unique_education = []
        seen = set()
        for edu in education:
            key = (edu["degree"], edu["major"])
            if key not in seen:
                seen.add(key)
                unique_education.append(edu)
                
        return unique_education

    def extract_experience_years(self, text: str) -> float:
        text_lower = text.lower()
        
        exp_patterns = [
            r'(\d+(?:\.\d+)?)\s*\+?\s*years?\s+(?:of\s+)?experience',
            r'experience\s*:\s*(\d+(?:\.\d+)?)\s*years?',
            r'(\d+(?:\.\d+)?)\s*years?\s+in\s+[a-zA-Z]'
        ]
        
        max_years = 0.0
        for pattern in exp_patterns:
            matches = re.findall(pattern, text_lower)
            for m in matches:
                try:
                    val = float(m)
                    if val > max_years:
                        max_years = val
                except ValueError:
                    pass
                    
        # Estimate from date ranges
        date_pattern = r'\b(19\d{2}|20\d{2})\s*[-–—]\s*(19\d{2}|20\d{2}|present|current)\b'
        date_matches = re.findall(date_pattern, text_lower)
        
        calculated_years = 0.0
        current_year = 2026
        
        for start, end in date_matches:
            try:
                start_year = float(start)
                end_year = current_year if end in ['present', 'current'] else float(end)
                diff = end_year - start_year
                if 0 < diff < 40:
                    calculated_years += diff
            except ValueError:
                pass
                
        est_years = max(max_years, round(calculated_years * 0.7, 1))
        return est_years if est_years > 0 else 0.5

    def extract_certifications(self, text: str) -> List[str]:
        text_lower = text.lower()
        found_certs = []
        for cert in self.CERTIFICATIONS_DB:
            if cert in text_lower:
                found_certs.append(cert.title())
        return list(set(found_certs))

    def extract_projects(self, text: str) -> List[str]:
        lines = text.split('\n')
        projects = []
        in_project_section = False
        project_header_patterns = [r'^projects$', r'^key projects$', r'^academic projects$', r'^personal projects$']
        
        for line in lines:
            line_strip = line.strip().lower()
            if any(re.match(pat, line_strip) for pat in project_header_patterns):
                in_project_section = True
                continue
            
            if in_project_section and line_strip in ['experience', 'education', 'skills', 'certifications', 'summary', 'languages', 'work history']:
                in_project_section = False
                
            if in_project_section and line.strip():
                if line.strip().startswith(('•', '-', '*', 'o')) or len(line.strip()) < 50:
                    proj = re.sub(r'^[•\-\*\s]+', '', line.strip())
                    if len(proj) > 10 and len(proj) < 150:
                        projects.append(proj)
                        if len(projects) >= 5:
                            break
                            
        return projects

    def analyze_resume_text(self, text: str) -> Dict[str, Any]:
        return {
            "emails": self.extract_emails(text),
            "phones": self.extract_phones(text),
            "skills": self.extract_skills(text),
            "education": self.extract_education(text),
            "experience_years": self.extract_experience_years(text),
            "certifications": self.extract_certifications(text),
            "projects": self.extract_projects(text)
        }
