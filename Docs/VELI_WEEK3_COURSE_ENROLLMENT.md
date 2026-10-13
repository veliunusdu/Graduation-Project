# Veli — Hafta 3 Course/Enrollment test hazırlığı ve sonuçları

> Pull öncesi tarihsel kayıt: Ayşe Week 3 kodu geldi ve önceki ön koşullar giderildi. Güncel sonuçlar [pull ve kontrol raporunda](VELI_WEEK3_AYSE_REVIEW_2026-10-13.md).

Tarih: 13 Ekim 2026. Kaynak: `AI_Odev_Platformu_Gorev_Dagilimi.pdf`, sayfa 3.

Kullanıcı kararı: **Testleri hazırla, Ayşe'nin kodunu bekleyelim.** Ürün kodu veya migration eklenmedi. Bu kayıt ödev platformuna aittir; DINOv3 projesiyle ilgili değildir.

## Görevin

- Bir lise ve bir üniversite course'u için test verisi hazırlamak.
- Öğrenciyi yalnızca bir derse kaydedip diğer dersi göremediğini doğrulamak.
- TeacherId sahiplik kontrolünü ve pasif course davranışını test etmek.
- Pozitif/negatif course ve enrollment senaryolarını kaydetmek.

## Hazırlanan test verisi

[JSON fixture](../samples/week3-course-enrollment/test-data.json): teacher-a / teacher-b ve student-a / student-b sentetik hesap referansları; teacher-a'nın HighSchool/9, teacher-b'nin University/1 aktif course'u. Yalnızca student-a lise dersine kayıtlı; student-b hiçbir derse kayıtlı değil. Her course için bir örnek ödev, pasif/yeniden aktif varyantları ve geçerli/geçersiz sınıf sınırları bulunur.

JSON bir import endpoint'i veya ASP.NET payload sözleşmesi değildir. FixtureKey/Ref alanları test referansıdır; gerçek kullanıcı/course Id değerleri izole test ortamında alınmalıdır. Kullanıcı parolası içermez. Mevcut otomatik testler kendi Alpha/Beta verilerini kullanır ve bu JSON'u import etmez.

## Çalıştırılan kontroller

**59 test geçti; 0 hata; 0 skipped (Debug).** [Ham TRX](TestEvidence/Week3/2026-10-13/veli-week3-existing-controls.trx) ve [makine okunabilir sonuçlar](VELI_WEEK3_RESULTS_2026-10-13.json).

Mevcut xUnit entegrasyon testleri WebApplicationFactory üzerinden gerçek Identity, cookie, antiforgery ve izole SQLite kullanır. Kontroller öğrenci ödev/teslim erişimi, TeacherId sahipliği, enrollment, yanlış rol ve değişmeyen DB sınırlarını doğrular. Varsayılan `web/App_Data/homework.db` bu testlerin veritabanı değildir.

Tekrar çalıştırma (PowerShell; proje kökünde):

```powershell
dotnet test 'tests\HomeworkPlatform.Web.Tests\HomeworkPlatform.Web.Tests.csproj' --no-restore --filter 'FullyQualifiedName~TeacherOwnershipTests|FullyQualifiedName~StudentOwnershipTests|FullyQualifiedName~AuthorizationMatrixTests' --logger 'trx;LogFileName=veli-week3-existing-controls-rerun.trx' --results-directory 'Docs\TestEvidence\Week3\2026-10-13' --verbosity minimal
```

İlk checkout'ta önce `dotnet restore HomeworkPlatform.sln` gerekir. Yeniden çalıştırma dosyası ilk kanıt dosyasını değiştirmez. Sonuç değişirse yeni tarihli sonuç kaydı oluştur.

## Senaryo matrisi

**7 senaryo mevcut kapsamda PASS; 6 senaryo BLOCKED.** PASS sonucu lise/üniversite alanlarını veya pasif course davranışını kapsamaz. `StudentWork` ödev listesidir; başarılı ödev izolasyonu ayrı öğrenci course listesi testi yerine geçmez.

| ID | Senaryo | Durum | Kapsam / ön koşul |
| --- | --- | --- | --- |
| W3-01 | Lise ve üniversite course verisi | BLOCKED | Course modelinde EducationLevel ve GradeLevel yok. |
| W3-02 | Sınıf/yıl aralığı doğrulaması | BLOCKED | Eğitim seviyesi ve sınıf/yıl model/form doğrulaması henüz yok. |
| W3-03 | Öğrenci yalnızca kayıtlı dersini listeler | BLOCKED | Öğrenci course listesi akışı mevcut değil; /StudentWork yalnızca ödev listeler. |
| W3-04 | Kayıtsız course doğrudan erişimi | BLOCKED | Öğrenci course detail endpoint/form akışı yok; /Courses öğretmen rolüne ayrılmıştır. |
| W3-05 | Kayıtsız dersin ödevine/teslimine erişim engeli | PASS | Mevcut course/ödev/enrollment sınırında geçti. |
| W3-06 | Öğretmenin kendi course CRUD akışı | PASS | Mevcut course/ödev/enrollment sınırında geçti. |
| W3-07 | Başka öğretmenin course kaydına erişim | PASS | Mevcut course/ödev/enrollment sınırında geçti. |
| W3-08 | Sahte TeacherId veya body Id | PASS | Mevcut course/ödev/enrollment sınırında geçti. |
| W3-09 | Pozitif enrollment erişimi açar | PASS | Mevcut course/ödev/enrollment sınırında geçti. |
| W3-10 | Yetkisiz, hatalı ve tekrarlı enrollment | PASS | Mevcut course/ödev/enrollment sınırında geçti. |
| W3-11 | Pasif course liste/detail davranışı | BLOCKED | IsActive alanı yok; pasif course görünürlük politikası tanımlanmamış. |
| W3-12 | Pasif course enrollment/submission davranışı | BLOCKED | IsActive alanı yok; pasif course yazma/önceki kayıt politikası tanımlanmamış. |
| W3-13 | Rol ve form güvenliği | PASS | Mevcut course/ödev/enrollment sınırında geçti. |

## Uygulama hazır olduğunda izlenecek adımlar

### W3-01 — Lise ve üniversite course verisi

Durum: **BLOCKED**.

- teacher-a ile HighSchool/9, teacher-b ile University/1 oluştur; DB ve formdaki EducationLevel/GradeLevel değerlerini karşılaştır.
- Ön koşul: Course modelinde EducationLevel ve GradeLevel yok.

### W3-02 — Sınıf/yıl aralığı doğrulaması

Durum: **BLOCKED**.

- HighSchool 9/12 ve University 1/4 geçerli; fixture içindeki dört sınır dışı değeri dene; geçersiz değer DB değiştirmemeli.
- Ön koşul: Eğitim seviyesi ve sınıf/yıl model/form doğrulaması henüz yok.

### W3-03 — Öğrenci yalnızca kayıtlı dersini listeler

Durum: **BLOCKED**.

- student-a yalnızca highschool-9 kaydına sahip; öğrenci ders listesinde lise görünmeli, university-1 görünmemeli.
- Ön koşul: Öğrenci course listesi akışı mevcut değil; /StudentWork yalnızca ödev listeler.

### W3-04 — Kayıtsız course doğrudan erişimi

Durum: **BLOCKED**.

- Öğrenci course detail akışı hazır olduğunda student-a ile university-1 gerçek route Id değerini çağır; ders bilgisi sızmamalı.
- Ön koşul: Öğrenci course detail endpoint/form akışı yok; /Courses öğretmen rolüne ayrılmıştır.

### W3-05 — Kayıtsız dersin ödevine/teslimine erişim engeli

Durum: **PASS**.

- StudentWork listesinde kayıtlı ödev görünür, diğer ödev görünmez; kayıtsız detail/Create GET ve POST 404, DB değişmez.
- Kanıt: `HomeworkPlatform.Web.Tests.StudentOwnershipTests.ListsAndCreationRequireEnrollmentAndDuplicateDoesNotOverwrite`

### W3-06 — Öğretmenin kendi course CRUD akışı

Durum: **PASS**.

- Öğretmen kendi course oluşturur/düzenler/siler; oluşturulan kayıt giriş yapan öğretmene bağlanır.
- Kanıt: `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ListsAndAssignmentCreationStayUnderOwnedCourses`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.OwnCrudIgnoresForgedOwnerParentAndBodyId`

### W3-07 — Başka öğretmenin course kaydına erişim

Durum: **PASS**.

- Diğer öğretmenin detail/edit/delete GET ve edit/delete POST çağrılarını dene; 404 ve değişmeyen kayıt beklenir.
- Kanıt: `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherRecordIsNotReadable(controller: "Assignments", action: "Edit")`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherMutationDoesNotChangeData(controller: "Courses", action: "Delete")`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherRecordIsNotReadable(controller: "Courses", action: "Edit")`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherMutationDoesNotChangeData(controller: "Assignments", action: "Edit")`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherMutationDoesNotChangeData(controller: "Assignments", action: "Delete")`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherRecordIsNotReadable(controller: "Courses", action: "Delete")`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherRecordIsNotReadable(controller: "Assignments", action: "Details")`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherMutationDoesNotChangeData(controller: "Courses", action: "Edit")`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherRecordIsNotReadable(controller: "Assignments", action: "Submissions")`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherRecordIsNotReadable(controller: "Courses", action: "Details")`, `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.ForeignTeacherRecordIsNotReadable(controller: "Assignments", action: "Delete")`

### W3-08 — Sahte TeacherId veya body Id

Durum: **PASS**.

- Create/Edit formunda diğer TeacherId/Id değerlerini gönder; sahiplik ve route hedefi değişmemeli.
- Kanıt: `HomeworkPlatform.Web.Tests.TeacherOwnershipTests.OwnCrudIgnoresForgedOwnerParentAndBodyId`

### W3-09 — Pozitif enrollment erişimi açar

Durum: **PASS**.

- Course sahibi öğrenciyi kaydeder; önceden kayıtsız ödeve erişim ve teslim açılır; diğer öğretmen teslimi göremez.
- Kanıt: `HomeworkPlatform.Web.Tests.StudentOwnershipTests.EnrollmentEnablesStudentWorkAndTeacherReadStaysUnderOwnedCourse`

### W3-10 — Yetkisiz, hatalı ve tekrarlı enrollment

Durum: **PASS**.

- Foreign course kaydı 404; Teacher veya olmayan email reddedilir; aynı StudentCourse ikinci kez oluşmaz.
- Kanıt: `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.EnrollmentCannotTargetForeignCourseOrNonStudentAndDuplicateIsSafe`

### W3-11 — Pasif course liste/detail davranışı

Durum: **BLOCKED**.

- highschool-9 IsActive=false yap; kayıtlı öğrenci listesi ve doğrudan detail erişimini tanımlanan pasif ders kuralıyla karşılaştır; aktif hale getirip yeniden dene.
- Ön koşul: IsActive alanı yok; pasif course görünürlük politikası tanımlanmamış.

### W3-12 — Pasif course enrollment/submission davranışı

Durum: **BLOCKED**.

- Pasif course için yeni enrollment/teslim dene; eski StudentCourse ve Submission kayıtlarının korunmasını ve tanımlanan erişim kuralını doğrula.
- Ön koşul: IsActive alanı yok; pasif course yazma/önceki kayıt politikası tanımlanmamış.

### W3-13 — Rol ve form güvenliği

Durum: **PASS**.

- Student ile Teacher course/Enroll akışını ve eksik antiforgery token ile mutasyonları dene; yanlış rol 403, tokensiz POST 400 ve DB değişmez.
- Kanıt: `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotMutateResourcesEvenWithValidToken(teacher: False, controller: "Courses", action: "Delete")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.ResourcePostWithoutTokenIsRejectedWithoutMutation(controller: "Submissions", action: "Edit")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotMutateResourcesEvenWithValidToken(teacher: False, controller: "Assignments", action: "Edit")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotReadResourceLists(teacher: False, route: "/Assignments")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.ResourcePostWithoutTokenIsRejectedWithoutMutation(controller: "Assignments", action: "Delete")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotMutateResourcesEvenWithValidToken(teacher: True, controller: "Submissions", action: "Create")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotMutateResourcesEvenWithValidToken(teacher: False, controller: "Assignments", action: "Delete")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.ResourcePostWithoutTokenIsRejectedWithoutMutation(controller: "Courses", action: "Create")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotReadResourceLists(teacher: True, route: "/Submissions")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotMutateResourcesEvenWithValidToken(teacher: True, controller: "Submissions", action: "Edit")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.ResourcePostWithoutTokenIsRejectedWithoutMutation(controller: "Assignments", action: "Edit")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotMutateResourcesEvenWithValidToken(teacher: False, controller: "Assignments", action: "Create")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.ResourcePostWithoutTokenIsRejectedWithoutMutation(controller: "Submissions", action: "Delete")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.ResourcePostWithoutTokenIsRejectedWithoutMutation(controller: "Courses", action: "Edit")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotReadResourceLists(teacher: False, route: "/Courses")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.ResourcePostWithoutTokenIsRejectedWithoutMutation(controller: "Courses", action: "Enroll")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotReadResourceLists(teacher: True, route: "/StudentWork")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.ResourcePostWithoutTokenIsRejectedWithoutMutation(controller: "Assignments", action: "Create")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotMutateResourcesEvenWithValidToken(teacher: False, controller: "Courses", action: "Enroll")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.ResourcePostWithoutTokenIsRejectedWithoutMutation(controller: "Courses", action: "Delete")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotMutateResourcesEvenWithValidToken(teacher: False, controller: "Courses", action: "Create")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotMutateResourcesEvenWithValidToken(teacher: True, controller: "Submissions", action: "Delete")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.WrongRoleCannotMutateResourcesEvenWithValidToken(teacher: False, controller: "Courses", action: "Edit")`, `HomeworkPlatform.Web.Tests.AuthorizationMatrixTests.ResourcePostWithoutTokenIsRejectedWithoutMutation(controller: "Submissions", action: "Create")`

## Ayşe'nin kodundan sonra

1. Course EducationLevel/GradeLevel/IsActive alanları, migration ve formları hazır olmalı; eski verilerin korunması ayrıca kontrol edilmeli.
2. Öğrenci course listesi/detail route'ları ve enrollment akışı belli olmalı. `/Courses` mevcut sürümde Teacher-only; öğrenci listesi olarak kullanılamaz.
3. Pasif dersin öğrenci görünürlüğü, doğrudan erişimi, yeni enrollment/teslim ve eski kayıt davranışı netleşmeli. Beklenen HTTP kodu bu karardan sonra yazılır; burada tahmin edilmez.
4. JSON fixture izole ortamda hazırlanır; 6 BLOCKED senaryo üründe gerçekten çalıştırılır. Yeni sürüm/commit, HTTP ve DB kanıtları kaydedilir; mevcut 59 kontrol de yeniden çalıştırılır.

**Tamamlanan:** Veli'nin test verisi/senaryo hazırlığı ve mevcut kontrollerin kayıt altına alınması. **Bekleyen:** Ayşe'nin Week 3 özellikleri üzerinde eğitim seviyesi, course görünürlüğü ve pasif ders testlerinin uygulanması. Veli'nin tam Week 3 ürün testi henüz kapatılmadı.
