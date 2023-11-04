set "mislocat=%userprofile%\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\Multi\Dogfight\FTC-BattleOfBritain"
copy FTC-test.cs  "%mislocat%\bob-mis-000.cs"
if not exist "%mislocat%\modules\" mkdir "%mislocat%\modules\"
copy "modules\*.*" "%mislocat%\modules\*.*"
pause