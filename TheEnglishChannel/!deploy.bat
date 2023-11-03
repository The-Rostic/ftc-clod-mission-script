set "mislocat=%userprofile%\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\Multi\Dogfight\FTC-test"
copy FTC-test.cs  "%mislocat%\FTC-test.cs"
if not exist "%mislocat%\modules\" mkdir "%mislocat%\modules\"
copy "modules\*.*" "%mislocat%\modules\*.*"
pause